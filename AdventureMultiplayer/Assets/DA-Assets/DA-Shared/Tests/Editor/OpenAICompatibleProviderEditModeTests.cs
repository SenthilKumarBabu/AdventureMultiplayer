using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace DA_Assets.LLM.Tests.Editor
{
    public sealed class OpenAICompatibleProviderEditModeTests
    {
        [Test]
        public async Task TimeoutHandledGracefully()
        {
            LLMProviderConfig config = LLMProviderConfig.CreateRuntime(
                "OpenAI",
                OpenAICompatibleProvider.OpenAIBaseUrl,
                "sk-test",
                "gpt-4o-mini",
                timeoutSeconds: 1,
                maxRetries: 0);

            var provider = new OpenAICompatibleProvider(
                config,
                async (_, _, _, cancellationToken) =>
                {
                    await Task.Delay(5000, cancellationToken);

                    return new LLMTransportResult
                    {
                        IsSuccessStatusCode = true,
                        StatusCode = 200,
                        Body = "{\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"late\"}}]}"
                    };
                });

            LLMResponse response = await provider.CompleteChatAsync(LLMRequest.CreateUserPrompt("gpt-4o-mini", "timeout"), CancellationToken.None);

            Assert.That(response, Is.Not.Null);
            Assert.That(response.error, Does.Contain("timed out"));

            Object.DestroyImmediate(config);
        }
    }
}
