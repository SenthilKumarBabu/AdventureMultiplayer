using DA_Assets.LLM;
using UnityEditor;
using UnityEngine;
using System.Threading;

namespace DA_Assets.Shared
{
    [CustomEditor(typeof(LLMProviderConfig))]
    public class LLMProviderConfigEditor : UnityEditor.Editor
    {
        public enum LLMPreset
        {
            Custom,
            OpenAI,
            Gemini,
            Anthropic,
            Ollama,
            LMStudio,
            vLLM
        }

        private LLMPreset _currentPreset = LLMPreset.Custom;
        private bool _isTesting;

        private SerializedProperty _providerName;
        private SerializedProperty _baseUrl;
        private SerializedProperty _apiKey;
        private SerializedProperty _defaultModel;
        private SerializedProperty _timeoutSeconds;
        private SerializedProperty _maxRetries;
        private SerializedProperty _temperature;

        private void OnEnable()
        {
            _providerName = serializedObject.FindProperty("_providerName");
            _baseUrl = serializedObject.FindProperty("_baseUrl");
            _apiKey = serializedObject.FindProperty("_apiKey");
            _defaultModel = serializedObject.FindProperty("_defaultModel");
            _timeoutSeconds = serializedObject.FindProperty("_timeoutSeconds");
            _maxRetries = serializedObject.FindProperty("_maxRetries");
            _temperature = serializedObject.FindProperty("_temperature");
            
            DeterminePreset();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("LLM Configuration", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _currentPreset = (LLMPreset)EditorGUILayout.EnumPopup("Preset", _currentPreset);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyPreset(_currentPreset);
            }

            EditorGUILayout.Space();

            GUI.enabled = _currentPreset == LLMPreset.Custom;
            EditorGUILayout.PropertyField(_providerName, new GUIContent("Provider Name"));
            EditorGUILayout.PropertyField(_baseUrl, new GUIContent("Base URL"));
            GUI.enabled = true;

            _apiKey.stringValue = EditorGUILayout.PasswordField("API Key", _apiKey.stringValue);
            
            EditorGUILayout.PropertyField(_defaultModel, new GUIContent("Default Model"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_temperature, new GUIContent("Temperature"));
            EditorGUILayout.PropertyField(_timeoutSeconds, new GUIContent("Timeout (s)"));
            EditorGUILayout.PropertyField(_maxRetries, new GUIContent("Max Retries"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (GUILayout.Button("Test Connection", GUILayout.Height(30)))
            {
                TestConnection();
            }
        }

        private void DeterminePreset()
        {
            if (_baseUrl == null) return;
            string url = _baseUrl.stringValue;
            if (url == OpenAICompatibleProvider.OpenAIBaseUrl) _currentPreset = LLMPreset.OpenAI;
            else if (url == OpenAICompatibleProvider.GeminiBaseUrl) _currentPreset = LLMPreset.Gemini;
            else if (url == OpenAICompatibleProvider.AnthropicBaseUrl) _currentPreset = LLMPreset.Anthropic;
            else if (url == OpenAICompatibleProvider.OllamaBaseUrl) _currentPreset = LLMPreset.Ollama;
            else if (url == OpenAICompatibleProvider.LMStudioBaseUrl) _currentPreset = LLMPreset.LMStudio;
            else if (url == OpenAICompatibleProvider.VllmBaseUrl) _currentPreset = LLMPreset.vLLM;
            else _currentPreset = LLMPreset.Custom;
        }

        private void ApplyPreset(LLMPreset preset)
        {
            switch (preset)
            {
                case LLMPreset.OpenAI:
                    _providerName.stringValue = "OpenAI";
                    _baseUrl.stringValue = OpenAICompatibleProvider.OpenAIBaseUrl;
                    _defaultModel.stringValue = "gpt-5.5-instant";
                    break;
                case LLMPreset.Gemini:
                    _providerName.stringValue = "Gemini";
                    _baseUrl.stringValue = OpenAICompatibleProvider.GeminiBaseUrl;
                    _defaultModel.stringValue = "gemini-3.5-flash";
                    break;
                case LLMPreset.Anthropic:
                    _providerName.stringValue = "Anthropic";
                    _baseUrl.stringValue = OpenAICompatibleProvider.AnthropicBaseUrl;
                    _defaultModel.stringValue = "claude-4-5-haiku-latest";
                    break;
                case LLMPreset.Ollama:
                    _providerName.stringValue = "Ollama";
                    _baseUrl.stringValue = OpenAICompatibleProvider.OllamaBaseUrl;
                    _defaultModel.stringValue = "llama3";
                    break;
                case LLMPreset.LMStudio:
                    _providerName.stringValue = "LM Studio";
                    _baseUrl.stringValue = OpenAICompatibleProvider.LMStudioBaseUrl;
                    _defaultModel.stringValue = "local-model";
                    break;
                case LLMPreset.vLLM:
                    _providerName.stringValue = "vLLM";
                    _baseUrl.stringValue = OpenAICompatibleProvider.VllmBaseUrl;
                    _defaultModel.stringValue = "vllm-model";
                    break;
            }
        }

        private async void TestConnection()
        {
            if (_isTesting) return;
            
            var config = (LLMProviderConfig)target;
            if (string.IsNullOrWhiteSpace(config.ApiKey) && _currentPreset != LLMPreset.Ollama && _currentPreset != LLMPreset.LMStudio)
            {
                EditorUtility.DisplayDialog("Test Connection", "API Key is empty.", "OK");
                return;
            }

            _isTesting = true;
            try
            {
                using var client = LLMClient.FromConfig(config);
                var request = LLMRequest.CreateUserPrompt(config.DefaultModel, "Reply with 'OK'", 0f);
                var cts = new CancellationTokenSource(10000);
                
                var response = await client.CompleteChatAsync(request, cts.Token);
                
                if (response != null && response.choices != null && response.choices.Length > 0)
                {
                    EditorUtility.DisplayDialog("Test Connection", $"Success!\nResponse: {response.choices[0].message.content}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Test Connection", $"Failed!\nError: {response?.error}", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Test Connection", $"Exception: {ex.Message}", "OK");
            }
            finally
            {
                _isTesting = false;
            }
        }
    }
}
