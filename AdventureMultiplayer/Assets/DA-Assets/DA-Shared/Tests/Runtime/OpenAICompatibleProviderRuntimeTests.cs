using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace DA_Assets.LLM.Tests.Runtime
{
    public sealed class OpenAICompatibleProviderRuntimeTests
    {
        [Test]
        public async Task OpenAIProviderSendsCorrectHeaders()
        {
            LLMProviderConfig config = LLMProviderConfig.CreateRuntime(
                "OpenAI",
                OpenAICompatibleProvider.OpenAIBaseUrl,
                "sk-test",
                "gpt-4o-mini");

            string capturedUrl = null;
            Dictionary<string, string> capturedHeaders = null;

            var provider = new OpenAICompatibleProvider(
                config,
                (url, _, headers, _) =>
                {
                    capturedUrl = url;
                    capturedHeaders = new Dictionary<string, string>(headers);

                    return Task.FromResult(new LLMTransportResult
                    {
                        IsSuccessStatusCode = true,
                        StatusCode = 200,
                        Body = "{\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"ok\"}}]}"
                    });
                });

            LLMResponse response = await provider.CompleteChatAsync(LLMRequest.CreateUserPrompt("gpt-4o-mini", "hello"));

            Assert.That(response.error, Is.Null);
            Assert.That(capturedUrl, Is.EqualTo("https://api.openai.com/v1/chat/completions"));
            Assert.That(capturedHeaders["Authorization"], Is.EqualTo("Bearer sk-test"));
            Assert.That(capturedHeaders["Content-Type"], Is.EqualTo("application/json"));

            Object.DestroyImmediate(config);
        }

        [Test]
        public async Task GeminiProviderUsesCorrectBaseUrl()
        {
            LLMProviderConfig config = LLMProviderConfig.CreateRuntime(
                "Gemini",
                OpenAICompatibleProvider.GeminiBaseUrl,
                "gemini-key",
                "gemini-2.0-flash");

            string capturedUrl = null;

            var provider = new OpenAICompatibleProvider(
                config,
                (url, _, _, _) =>
                {
                    capturedUrl = url;

                    return Task.FromResult(new LLMTransportResult
                    {
                        IsSuccessStatusCode = true,
                        StatusCode = 200,
                        Body = "{\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"ok\"}}]}"
                    });
                });

            await provider.CompleteChatAsync(LLMRequest.CreateUserPrompt("gemini-2.0-flash", "hello"));

            Assert.That(capturedUrl, Is.EqualTo("https://generativelanguage.googleapis.com/v1beta/openai/chat/completions"));

            Object.DestroyImmediate(config);
        }

        [Test]
        public async Task RetryOnTransientFailure()
        {
            LLMProviderConfig config = LLMProviderConfig.CreateRuntime(
                "OpenAI",
                OpenAICompatibleProvider.OpenAIBaseUrl,
                "sk-test",
                "gpt-4o-mini",
                maxRetries: 2);

            int attempts = 0;

            var provider = new OpenAICompatibleProvider(
                config,
                (_, _, _, _) =>
                {
                    attempts++;

                    if (attempts == 1)
                    {
                        return Task.FromResult(new LLMTransportResult
                        {
                            IsSuccessStatusCode = false,
                            StatusCode = 503,
                            ErrorMessage = "Service Unavailable"
                        });
                    }

                    return Task.FromResult(new LLMTransportResult
                    {
                        IsSuccessStatusCode = true,
                        StatusCode = 200,
                        Body = "{\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"ok\"}}]}"
                    });
                },
                (_, _) => Task.CompletedTask);

            LLMResponse response = await provider.CompleteChatAsync(LLMRequest.CreateUserPrompt("gpt-4o-mini", "retry"));

            Assert.That(response.error, Is.Null);
            Assert.That(response.FirstMessageContent, Is.EqualTo("ok"));
            Assert.That(attempts, Is.EqualTo(2));

            Object.DestroyImmediate(config);
        }
    }
}
