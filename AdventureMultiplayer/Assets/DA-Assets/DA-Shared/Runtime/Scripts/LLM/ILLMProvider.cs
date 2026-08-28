using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.LLM
{
    public interface ILLMProvider
    {
        Task<LLMResponse> CompleteChatAsync(LLMRequest request, CancellationToken cancellationToken = default);
    }
}
