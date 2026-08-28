using DA_Assets.Constants;
using UnityEngine;

namespace DA_Assets.LLM
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/LLM Provider Config")]
    public class LLMProviderConfig : ScriptableObject
    {
        [SerializeField] string _providerName = "OpenAI";
        [SerializeField] string _baseUrl = OpenAICompatibleProvider.OpenAIBaseUrl;
        [SerializeField] string _apiKey;
        [SerializeField] string _defaultModel = "gpt-4o-mini";
        [SerializeField] int _timeoutSeconds = 60;
        [SerializeField] int _maxRetries = 3;
        [SerializeField] float _temperature = 0.3f;

        public string ProviderName
        {
            get => _providerName;
            set => _providerName = value;
        }

        public string BaseUrl
        {
            get => _baseUrl;
            set => _baseUrl = value;
        }

        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value;
        }

        public string DefaultModel
        {
            get => _defaultModel;
            set => _defaultModel = value;
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set => _timeoutSeconds = Mathf.Max(0, value);
        }

        public int MaxRetries
        {
            get => _maxRetries;
            set => _maxRetries = Mathf.Max(0, value);
        }

        public float Temperature
        {
            get => _temperature;
            set => _temperature = value;
        }

        public LLMProviderConfig CreateRuntimeCopy()
        {
            return CreateRuntime(
                ProviderName,
                BaseUrl,
                ApiKey,
                DefaultModel,
                TimeoutSeconds,
                MaxRetries,
                Temperature);
        }

        public void ApplyOverrides(string apiKeyOverride = null, string modelOverride = null)
        {
            if (!string.IsNullOrWhiteSpace(apiKeyOverride))
            {
                ApiKey = apiKeyOverride;
            }

            if (!string.IsNullOrWhiteSpace(modelOverride))
            {
                DefaultModel = modelOverride;
            }
        }

        public static LLMProviderConfig CreateRuntime(
            string providerName,
            string baseUrl,
            string apiKey,
            string defaultModel,
            int timeoutSeconds = 60,
            int maxRetries = 3,
            float temperature = 0.3f)
        {
            LLMProviderConfig config = CreateInstance<LLMProviderConfig>();
            config.hideFlags = HideFlags.HideAndDontSave;
            config.ProviderName = providerName;
            config.BaseUrl = baseUrl;
            config.ApiKey = apiKey;
            config.DefaultModel = defaultModel;
            config.TimeoutSeconds = timeoutSeconds;
            config.MaxRetries = maxRetries;
            config.Temperature = temperature;
            return config;
        }
    }
}
