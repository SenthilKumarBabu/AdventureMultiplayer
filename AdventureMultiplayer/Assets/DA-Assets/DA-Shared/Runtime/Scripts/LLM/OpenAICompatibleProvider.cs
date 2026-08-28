using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DA_Assets.LLM
{
    public delegate Task<LLMTransportResult> LLMTransportHandler(
        string url,
        string jsonBody,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken);

    public sealed class LLMTransportResult
    {
        public bool IsSuccessStatusCode { get; set; }
        public long StatusCode { get; set; }
        public string Body { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class OpenAICompatibleProvider : ILLMProvider
    {
        public const string OpenAIBaseUrl = "https://api.openai.com/v1";
        public const string GeminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai";
        public const string AnthropicBaseUrl = "https://api.anthropic.com/v1";
        public const string OllamaBaseUrl = "http://localhost:11434/v1";
        public const string LMStudioBaseUrl = "http://localhost:1234/v1";
        public const string VllmBaseUrl = "http://localhost:8000/v1";

        private readonly LLMProviderConfig _config;
        private readonly LLMTransportHandler _transport;
        private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

        public OpenAICompatibleProvider(
            LLMProviderConfig config,
            LLMTransportHandler transport = null,
            Func<TimeSpan, CancellationToken, Task> delayAsync = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _transport = transport ?? SendWithUnityWebRequestAsync;
            _delayAsync = delayAsync ?? Task.Delay;
        }

        public async Task<LLMResponse> CompleteChatAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return LLMResponse.FromError("LLM request is null.");
            }

            if (string.IsNullOrWhiteSpace(_config.BaseUrl))
            {
                return LLMResponse.FromError("LLM provider base URL is not configured.");
            }

            if (string.IsNullOrWhiteSpace(request.model))
            {
                request.model = _config.DefaultModel;
            }

            if (request.messages == null || request.messages.Length == 0)
            {
                return LLMResponse.FromError("LLM request does not contain any messages.");
            }

            string url = BuildChatCompletionsUrl(_config.BaseUrl);
            string jsonBody = JsonUtility.ToJson(request);
            Dictionary<string, string> headers = BuildHeaders(_config.ApiKey);
            int maxAttempts = Math.Max(1, _config.MaxRetries + 1);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                CancellationTokenSource timeoutCts = _config.TimeoutSeconds > 0
                    ? new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds))
                    : null;

                CancellationTokenSource linkedCts = timeoutCts != null
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
                    : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    LLMTransportResult transportResult = await _transport(url, jsonBody, headers, linkedCts.Token);

                    if (transportResult == null)
                    {
                        if (attempt < maxAttempts - 1)
                        {
                            await DelayBeforeRetryAsync(attempt, cancellationToken);
                            continue;
                        }

                        return LLMResponse.FromError("LLM provider returned an empty transport result.");
                    }

                    if (transportResult.IsSuccessStatusCode)
                    {
                        return ParseResponse(transportResult.Body);
                    }

                    if (attempt < maxAttempts - 1 && IsTransientStatusCode(transportResult.StatusCode))
                    {
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    return ParseErrorResponse(transportResult);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts != null && timeoutCts.IsCancellationRequested)
                {
                    return LLMResponse.FromError("LLM request timed out.");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempt < maxAttempts - 1)
                    {
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    return LLMResponse.FromError(ex.Message);
                }
                finally
                {
                    linkedCts.Dispose();
                    timeoutCts?.Dispose();
                }
            }

            return LLMResponse.FromError("LLM request failed after retries.");
        }

        private async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
        {
            int delayMs = 200 * (attempt + 1);
            await _delayAsync(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
        }

        private static string BuildChatCompletionsUrl(string baseUrl)
        {
            string normalized = baseUrl?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            if (normalized.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            return normalized + "/chat/completions";
        }

        private static Dictionary<string, string> BuildHeaders(string apiKey)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                headers["Authorization"] = "Bearer " + apiKey;
            }

            return headers;
        }

        private static bool IsTransientStatusCode(long statusCode)
        {
            return statusCode == 0 ||
                   statusCode == 408 ||
                   statusCode == 429 ||
                   statusCode == 500 ||
                   statusCode == 502 ||
                   statusCode == 503 ||
                   statusCode == 504;
        }

        private static LLMResponse ParseResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return LLMResponse.FromError("LLM provider returned an empty response.");
            }

            LLMResponse response = JsonUtility.FromJson<LLMResponse>(json);
            if (response?.choices != null && response.choices.Length > 0)
            {
                return response;
            }

            LLMErrorEnvelope errorEnvelope = JsonUtility.FromJson<LLMErrorEnvelope>(json);
            if (!string.IsNullOrWhiteSpace(errorEnvelope?.error?.message))
            {
                return LLMResponse.FromError(errorEnvelope.error.message);
            }

            return LLMResponse.FromError("LLM provider returned an invalid response structure.");
        }

        private static LLMResponse ParseErrorResponse(LLMTransportResult transportResult)
        {
            if (!string.IsNullOrWhiteSpace(transportResult?.Body))
            {
                LLMErrorEnvelope errorEnvelope = JsonUtility.FromJson<LLMErrorEnvelope>(transportResult.Body);
                if (!string.IsNullOrWhiteSpace(errorEnvelope?.error?.message))
                {
                    return LLMResponse.FromError(errorEnvelope.error.message);
                }
            }

            string error = !string.IsNullOrWhiteSpace(transportResult?.ErrorMessage)
                ? transportResult.ErrorMessage
                : $"LLM request failed with HTTP {(transportResult?.StatusCode ?? 0)}.";

            return LLMResponse.FromError(error);
        }

        private static async Task<LLMTransportResult> SendWithUnityWebRequestAsync(
            string url,
            string jsonBody,
            Dictionary<string, string> headers,
            CancellationToken cancellationToken)
        {
            using UnityWebRequest webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? string.Empty);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    webRequest.SetRequestHeader(header.Key, header.Value);
                }
            }

            UnityWebRequestAsyncOperation requestOperation = webRequest.SendWebRequest();

            while (!requestOperation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    webRequest.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }

            bool isSuccess;
#if UNITY_2020_1_OR_NEWER
            isSuccess = webRequest.result == UnityWebRequest.Result.Success;
#else
            isSuccess = !webRequest.isNetworkError && !webRequest.isHttpError;
#endif

            return new LLMTransportResult
            {
                IsSuccessStatusCode = isSuccess,
                StatusCode = webRequest.responseCode,
                Body = webRequest.downloadHandler?.text,
                ErrorMessage = isSuccess ? null : webRequest.error
            };
        }
    }
}
