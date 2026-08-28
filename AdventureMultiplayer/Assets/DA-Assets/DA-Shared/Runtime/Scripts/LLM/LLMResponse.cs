using System;

namespace DA_Assets.LLM
{
    [Serializable]
    public sealed class LLMResponse
    {
        public string id;
        public string model;
        public LLMChoice[] choices;
        public LLMUsage usage;
        public string error;

        public string FirstMessageContent =>
            choices != null && choices.Length > 0
                ? choices[0]?.message?.content
                : null;

        public static LLMResponse FromError(string error)
        {
            return new LLMResponse
            {
                error = error
            };
        }
    }

    [Serializable]
    public sealed class LLMChoice
    {
        public int index;
        public LLMMessage message;
        public string finish_reason;
    }

    [Serializable]
    public sealed class LLMUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    #pragma warning disable CS0649
    [Serializable]
    internal sealed class LLMErrorEnvelope
    {
        public LLMError error;
    }

    [Serializable]
    internal sealed class LLMError
    {
        public string message;
        public string type;
        public string code;
    }
    #pragma warning restore CS0649
}
