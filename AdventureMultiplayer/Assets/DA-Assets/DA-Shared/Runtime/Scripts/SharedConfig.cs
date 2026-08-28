using DA_Assets.Constants;
using DA_Assets.LLM;
using DA_Assets.Singleton;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DA_Assets.Shared
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/Shared Config")]
    public class SharedConfig : AssetConfig<SharedConfig>
    {
        [Header("Changelog")]
        [SerializeField] bool _useTestChangelog;
        [SerializeField] TextAsset _changelogTestJson;
        public static bool UseTestChangelog => Instance._useTestChangelog;
        public static TextAsset ChangelogTestJson => Instance._changelogTestJson;

        [Header("Docs Getter")]
        [SerializeField] DocsGetterSettings docsGetterSettings = DocsGetterSettings.Default;
        public static DocsGetterSettings DocsGetterSettings => Instance.docsGetterSettings;

        [Header("LLM")]
        [SerializeField] LLMProviderConfig _defaultLlmProviderConfig;
        public static LLMProviderConfig DefaultLlmProviderConfig => Instance._defaultLlmProviderConfig;
    }

    [Serializable]
    public struct DocsGetterSettings
    {
        public Object PythonDocsGetter;
        public int CacheExpiryHours;
        public string UserAgent;
        public int TimeoutSeconds;
        public bool CacheEnabled;

        public static DocsGetterSettings Default => new DocsGetterSettings
        {
            PythonDocsGetter = null,
            CacheExpiryHours = 720,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            TimeoutSeconds = 30,
            CacheEnabled = true
        };
    }
}
