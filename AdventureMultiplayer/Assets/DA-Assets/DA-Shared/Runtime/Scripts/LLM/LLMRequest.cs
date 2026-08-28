using System;

namespace DA_Assets.LLM
{
    [Serializable]
    public sealed class LLMRequest
    {
        public string model;
        public LLMMessage[] messages;
        public float temperature = 0.3f;

        public static LLMRequest CreateUserPrompt(string model, string prompt, float temperature = 0.3f)
        {
            return new LLMRequest
            {
                model = model,
                temperature = temperature,
                messages = new[]
                {
                    LLMMessage.User(prompt)
                }
            };
        }
    }

    [Serializable]
    public sealed class LLMMessage
    {
        public string role;
        public string content;

        public static LLMMessage User(string content)
        {
            return new LLMMessage
            {
                role = "user",
                content = content
            };
        }
    }
}
