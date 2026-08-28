using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.LLM
{
    public sealed class LLMClient : IDisposable
    {
        private readonly bool _ownsConfig;

        public LLMProviderConfig Config { get; }
        public ILLMProvider Provider { get; }

        public LLMClient(LLMProviderConfig config, ILLMProvider provider = null, bool ownsConfig = true)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Provider = provider ?? new OpenAICompatibleProvider(Config);
            _ownsConfig = ownsConfig;
        }

        public Task<LLMResponse> CompleteChatAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            return Provider.CompleteChatAsync(request, cancellationToken);
        }

        public static LLMClient FromConfig(LLMProviderConfig config, string apiKeyOverride = null, string modelOverride = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            LLMProviderConfig runtimeConfig = config.CreateRuntimeCopy();
            runtimeConfig.ApplyOverrides(apiKeyOverride, modelOverride);
            return new LLMClient(runtimeConfig, ownsConfig: true);
        }

        public static LLMClient CreateOpenAI(string apiKey, string model, int timeoutSeconds = 60, int maxRetries = 3, float temperature = 0.3f)
        {
            LLMProviderConfig config = LLMProviderConfig.CreateRuntime(
                "OpenAI",
                OpenAICompatibleProvider.OpenAIBaseUrl,
                apiKey,
                model,
                timeoutSeconds,
                maxRetries,
                temperature);

            return new LLMClient(config, ownsConfig: true);
        }

        public void Dispose()
        {
            if (_ownsConfig && Config != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(Config);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(Config);
                }
            }
        }
    }
}
