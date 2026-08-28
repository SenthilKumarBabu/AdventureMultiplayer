using DA_Assets.DAI;
using DA_Assets.Shared;
using DA_Assets.Shared.Extensions;
using DA_Assets.Singleton;
using DA_Assets.Tools;
using DA_Assets.UpdateChecker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace DA_Assets.UpdateChecker
{
    public static class UpdateService
    {
        public static event Action OnConfigLoaded;

        private static readonly string ConfigUrl = "https://da-assets.github.io/site/files/webConfig.json";
        private static WebConfig _webConfig;
        private static bool _isConfigLoaded = false;
        private static bool _isLoading = false;

        static UpdateService()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (_isConfigLoaded || _isLoading)
            {
                return;
            }
            LoadWebConfig();
        }

        private static async void LoadWebConfig()
        {
            _isLoading = true;

            if (SharedConfig.UseTestChangelog)
            {
                if (SharedConfig.ChangelogTestJson == null)
                {
                    Debug.LogError("SharedConfig is configured to use the test changelog, but no changelog JSON asset is assigned.");
                    _isLoading = false;
                    return;
                }

                ParseWebConfig(SharedConfig.ChangelogTestJson.text);
                _isLoading = false;
                return;
            }

            using (var request = UnityWebRequest.Get(ConfigUrl))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Delay(50);
                }

#if UNITY_2020_1_OR_NEWER
                if (request.result == UnityWebRequest.Result.Success)
#else
                if (!request.isNetworkError && !request.isHttpError)
#endif
                {
                    ParseWebConfig(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError(SharedLocKey.log_update_checker_load_failed.Localize(request.error));
                }
            }
            _isLoading = false;
        }

        private static void ParseWebConfig(string json)
        {
            try
            {
                _webConfig = JsonUtility.FromJson<Models.WebConfig>(json);
                _isConfigLoaded = true;
                OnConfigLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError(SharedLocKey.log_update_checker_parse_failed.Localize(ex));
            }
        }

        public static VersionInfo GetVersionInfo(Models.AssetType assetType, string currentVersionStr)
        {
            if (!_isConfigLoaded)
            {
                Initialize();
                return null;
            }

            var assetConfig = _webConfig.Assets.FirstOrDefault(a => a.Type == assetType);
            if (assetConfig.Versions == null || assetConfig.Versions.Count == 0)
            {
                return null;
            }

            var currentAssetVersion = assetConfig.Versions.FirstOrDefault(v => v.Version.Equals(currentVersionStr, StringComparison.OrdinalIgnoreCase));
            var latestAssetVersion = assetConfig.Versions.LastOrDefault();

            if (string.IsNullOrEmpty(currentAssetVersion.Version) || string.IsNullOrEmpty(latestAssetVersion.Version))
            {
                return null;
            }

            return new VersionInfo
            {
                AssetConfig = assetConfig,
                CurrentVersion = currentAssetVersion,
                LatestVersion = latestAssetVersion
            };
        }

        public static List<Models.DeveloperMessage> GetDeveloperMessages(Models.AssetType assetType, string currentVersionStr, DALanguage lang)
        {
            if (!_isConfigLoaded)
            {
                return null;
            }

            var assetConfig = _webConfig.Assets.FirstOrDefault(a => a.Type == assetType);
            if (assetConfig.Versions == null)
            {
                return null;
            }

            var messages = new List<Models.DeveloperMessage>();

            if (assetConfig.DeveloperMessage.HasText(lang))
            {
                messages.Add(assetConfig.DeveloperMessage);
            }

            var currentAssetVersion = assetConfig.Versions.FirstOrDefault(v => v.Version.Equals(currentVersionStr, StringComparison.OrdinalIgnoreCase));
            if (currentAssetVersion.DeveloperMessage.HasText(lang))
            {
                messages.Add(currentAssetVersion.DeveloperMessage);
            }

            return messages;
        }

        public static Models.AssetVersion? GetCurrentAssetVersion(Models.AssetType assetType, string currentVersionStr)
        {
            if (!_isConfigLoaded)
            {
                return null;
            }

            var assetConfig = _webConfig.Assets.FirstOrDefault(a => a.Type == assetType);
            if (assetConfig.Versions == null || assetConfig.Versions.Count == 0)
            {
                return null;
            }

            return assetConfig.Versions.FirstOrDefault(v => v.Version.Equals(currentVersionStr, StringComparison.OrdinalIgnoreCase));
        }

        public static int GetFirstVersionDaysCount(Models.AssetType assetType)
        {
            if (!_isConfigLoaded)
            {
                return -1;
            }

            var assetConfig = _webConfig.Assets.FirstOrDefault(a => a.Type == assetType);
            if (assetConfig.Versions == null || assetConfig.Versions.Count == 0)
            {
                return -1;
            }

            var firstVersion = assetConfig.Versions.FirstOrDefault();
            if (string.IsNullOrEmpty(firstVersion.ReleaseDate))
            {
                return -1;
            }

            if (DateTime.TryParseExact(firstVersion.ReleaseDate, "MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime firstReleaseDate))
            {
                return (int)Math.Abs((DateTime.Now - firstReleaseDate).TotalDays);
            }

            return -1;
        }
    }

    public static class VersionStatusEmojis
    {
        private static readonly System.Random _random = new System.Random();

        public static class Current
        {
            private static readonly string[] Stable =
            {
                "✅", "😎", "👌", "💪", "⭐", "👍"
            };

            private static readonly string[] Beta =
            {
                "🛠️", "🤔", "👀", "🤞", "🥚"
            };

            private static readonly string[] Buggy =
            {
                "🐞", "⚠️", "❌", "😵‍💫", "😫", "😐", "🚧", "🤕", "☢️"
            };

            public static string GetRandom(Models.VersionType type)
            {
                switch (type)
                {
                    case Models.VersionType.stable:
                        return Stable[_random.Next(Stable.Length)];
                    case Models.VersionType.beta:
                        return Beta[_random.Next(Beta.Length)];
                    case Models.VersionType.buggy:
                        return Buggy[_random.Next(Buggy.Length)];
                    default:
                        return "";
                }
            }
        }

        public static class Latest
        {
            private static readonly string[] Stable =
            {
                "🚀", "✨", "🔥", "🎉", "🥳"
            };

            private static readonly string[] Beta =
            {
                "🛠️", "🤔", "👀", "🤞", "🥚"
            };

            private static readonly string[] Buggy =
            {
                "🐞", "⚠️", "❌", "😵‍💫", "😫", "😐", "🚧", "🤕", "☢️"
            };

            public static string GetRandom(Models.VersionType type)
            {
                switch (type)
                {
                    case Models.VersionType.stable:
                        return Stable[_random.Next(Stable.Length)];
                    case Models.VersionType.beta:
                        return Beta[_random.Next(Beta.Length)];
                    case Models.VersionType.buggy:
                        return Buggy[_random.Next(Buggy.Length)];
                    default:
                        return "";
                }
            }
        }
    }

    public class VersionDisplayElement : VisualElement
    {
        private readonly Models.AssetType _assetType;
        private readonly string _currentVersionStr;
        private readonly DALanguage _lang;
        private readonly DAInspectorUITK _uitk;

        public VersionDisplayElement(Models.AssetType assetType, string currentVersionStr, DALanguage lang, DAInspectorUITK uitk)
        {
            _assetType = assetType;
            _currentVersionStr = currentVersionStr;
            _lang = lang;
            _uitk = uitk;

            style.marginTop = 5;
            style.marginBottom = 5;

            UpdateService.OnConfigLoaded += Render;
            RegisterCallback<DetachFromPanelEvent>(evt => UpdateService.OnConfigLoaded -= Render);

            Render();
        }

        private void Render()
        {
            Clear();
            var versionInfo = UpdateService.GetVersionInfo(_assetType, _currentVersionStr);

            if (versionInfo == null)
            {
                var titleLabel = new Label(SharedLocKey.label_your_version.Localize(_lang, _assetType) + " " + _currentVersionStr);
                Add(titleLabel);
                return;
            }

            var container = new VisualElement();
            Add(container);

            RenderCurrentVersion(container, versionInfo);

            if (versionInfo.CurrentVersion.Version != versionInfo.LatestVersion.Version)
            {
                RenderLatestVersion(container, versionInfo);
            }
        }

        private void RenderCurrentVersion(VisualElement parent, VersionInfo info)
        {
            var horizontalLayout = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var titleLabel = new Label(SharedLocKey.label_your_version.Localize(_lang, _assetType))
            {
                style =
                {
                    marginRight = 5,
                    flexShrink = 0
                }
            };
            horizontalLayout.Add(titleLabel);

            string statusText = info.CurrentVersion.Version;
            Color statusColor;

            bool isUpToDate = info.CurrentVersion.Version == info.LatestVersion.Version;
            var currentType = info.CurrentVersion.VersionType;

            string emoji = VersionStatusEmojis.Current.GetRandom(currentType);
            string statusName = null;

            switch (currentType)
            {
                case Models.VersionType.stable:
                    statusName = SharedLocKey.label_stable_version.Localize(_lang);
                    statusColor = isUpToDate
                        ? _uitk.ColorScheme.GREEN
                        : _uitk.ColorScheme.TEXT;
                    break;
                case Models.VersionType.beta:
                    statusName = SharedLocKey.label_beta_version.Localize(_lang);
                    statusColor = _uitk.ColorScheme.ORANGE;
                    break;
                case Models.VersionType.buggy:
                    statusName = SharedLocKey.label_buggy_version.Localize(_lang);
                    statusColor = _uitk.ColorScheme.RED;
                    break;
                default:
                    statusColor = _uitk.ColorScheme.TEXT;
                    break;
            }

            if (statusName != null)
            {
                string emojiPart = EngineDetector.IsTuanjie ? "" : $" {emoji}";
                statusText += $" ({statusName}{emojiPart})";
            }

            var valueLabel = new Label(statusText)
            {
                style =
                {
                    color = new StyleColor(statusColor),
                    flexShrink = 0,
                    whiteSpace = WhiteSpace.NoWrap
                }
            };
            RegisterChangelogPopup(valueLabel, info.CurrentVersion, statusText);
            horizontalLayout.Add(valueLabel);
            parent.Add(horizontalLayout);
        }

        private void RenderLatestVersion(VisualElement parent, VersionInfo info)
        {
            var horizontalLayout = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 2 } };

            var titleLabel = new Label(SharedLocKey.label_latest_version.Localize(_lang, _assetType))
            {
                style =
                {
                    marginRight = 5,
                    flexShrink = 0
                }
            };
            horizontalLayout.Add(titleLabel);

            string statusText = info.LatestVersion.Version;
            string emoji = VersionStatusEmojis.Latest.GetRandom(info.LatestVersion.VersionType);
            var latestType = info.LatestVersion.VersionType;
            string statusName = null;

            switch (latestType)
            {
                case Models.VersionType.stable:
                    statusName = SharedLocKey.label_stable_version.Localize(_lang);
                    break;
                case Models.VersionType.beta:
                    statusName = SharedLocKey.label_beta_version.Localize(_lang);
                    break;
                case Models.VersionType.buggy:
                    statusName = SharedLocKey.label_buggy_version.Localize(_lang);
                    break;
            }

            if (statusName != null)
            {
                string emojiPart = EngineDetector.IsTuanjie ? "" : $" {emoji}";
                statusText += $" ({statusName}{emojiPart})";
            }

            var valueLabel = new Label(statusText)
            {
                style =
                {
                    color = new StyleColor(_uitk.ColorScheme.TEXT_SECOND),
                    flexShrink = 0,
                    whiteSpace = WhiteSpace.NoWrap
                }
            };
            RegisterChangelogPopup(valueLabel, info.LatestVersion, statusText);
            horizontalLayout.Add(valueLabel);
            parent.Add(horizontalLayout);
        }

        private void RegisterChangelogPopup(Label label, Models.AssetVersion version, string statusText)
        {
            label.RegisterCallback<PointerEnterEvent>(_ =>
            {
                Rect screenRect = GUIUtility.GUIToScreenRect(label.worldBound);
                ChangelogPopupWindow.Show(screenRect, _uitk, version, statusText);
            });
        }
    }

    public class DeveloperMessagesElement : VisualElement
    {
        private readonly Models.AssetType _assetType;
        private readonly string _currentVersionStr;
        private readonly DALanguage _lang;
        private readonly DAInspectorUITK _uitk;

        public DeveloperMessagesElement(Models.AssetType assetType, string currentVersionStr, DALanguage lang, DAInspectorUITK uitk)
        {
            _assetType = assetType;
            _currentVersionStr = currentVersionStr;
            _lang = lang;
            _uitk = uitk;

            UpdateService.OnConfigLoaded += Render;
            RegisterCallback<DetachFromPanelEvent>(evt => UpdateService.OnConfigLoaded -= Render);

            Render();
        }

        private void Render()
        {
            Clear();
            var messages = UpdateService.GetDeveloperMessages(_assetType, _currentVersionStr, _lang);
            if (messages == null || messages.Count == 0)
            {
                return;
            }

            foreach (var msg in messages)
            {
                var helpBox = new CustomHelpBox(_uitk, new HelpBoxData
                {
                    Message = msg.GetText(_lang),
                    MessageType = msg.Type,
                    Links = BuildLinks(msg.Links),
                    OnClick = null
                });
                helpBox.style.marginBottom = 5;
                helpBox.style.marginTop = 5;
                Add(helpBox);
            }
        }

        private Dictionary<string, string> BuildLinks(List<Models.ChangelogLink> links)
        {
            if (links == null || links.Count == 0)
            {
                return null;
            }

            var result = new Dictionary<string, string>();

            for (int i = 0; i < links.Count; i++)
            {
                result[links[i].Id] = links[i].Url;
            }

            return result;
        }
    }

    public class VersionInfo
    {
        public Models.AssetConfig AssetConfig { get; set; }
        public Models.AssetVersion CurrentVersion { get; set; }
        public Models.AssetVersion LatestVersion { get; set; }
    }

    namespace Models
    {
        public enum AssetType
        {
            FCU = 1,
            DAB = 2,
            UITK_CONV = 3,
            DAL = 4,
            IMG_OVF = 5,
            UITK_LNK = 6,
            FIGMAGE = 7,
            FZPE = 8,
            DAMCP = 9,
            MGOCU = 10
        }

        public enum VersionType { stable = 0, beta = 1, buggy = 2 }
        public enum MessageType { None, Info, Warning, Error }

        [Serializable]
        public struct WebConfig
        {
            [SerializeField] private List<AssetConfig> assets;
            public List<AssetConfig> Assets => assets;
        }

        [Serializable]
        public struct AssetConfig
        {
            [SerializeField] private string name;
            [SerializeField] private AssetType assetType;
            [SerializeField] private DeveloperMessage developerMessage;
            [SerializeField] private List<AssetVersion> versions;

            public string Name => name;
            public AssetType Type => assetType;
            public DeveloperMessage DeveloperMessage => developerMessage;
            public List<AssetVersion> Versions => versions;
        }

        [Serializable]
        public struct AssetVersion
        {
            [SerializeField] private string version;
            [SerializeField] private VersionType versionType;
            [SerializeField] private string releaseDate;
            [SerializeField] private string description;
            [SerializeField] private ChangelogRelease changelog;
            [SerializeField] private DeveloperMessage developerMessage;

            public string Version => version;
            public VersionType VersionType => versionType;
            public string ReleaseDate => releaseDate;
            public string Description => description;
            public ChangelogRelease Changelog => changelog;
            public DeveloperMessage DeveloperMessage => developerMessage;
        }

        [Serializable]
        public struct ChangelogRelease
        {
            [SerializeField] private List<ChangelogItem> added;
            [SerializeField] private List<ChangelogItem> changed;
            [SerializeField] private List<ChangelogItem> deprecated;
            [SerializeField] private List<ChangelogItem> removed;
            [SerializeField] private List<ChangelogItem> @fixed;
            [SerializeField] private List<ChangelogItem> security;
            [SerializeField] private List<ChangelogItem> notes;
            [SerializeField] private List<ChangelogItem> knownIssues;

            public List<ChangelogItem> Added => added;
            public List<ChangelogItem> Changed => changed;
            public List<ChangelogItem> Deprecated => deprecated;
            public List<ChangelogItem> Removed => removed;
            public List<ChangelogItem> Fixed => @fixed;
            public List<ChangelogItem> Security => security;
            public List<ChangelogItem> Notes => notes;
            public List<ChangelogItem> KnownIssues => knownIssues;
        }

        [Serializable]
        public struct ChangelogItem
        {
            [SerializeField] private string description;
            [SerializeField] private string richText;
            [SerializeField] private List<ChangelogLink> links;

            public string Description => description;
            public string RichText => richText;
            public List<ChangelogLink> Links => links;
        }

        [Serializable]
        public struct ChangelogLink
        {
            [SerializeField] private string id;
            [SerializeField] private string url;

            public string Id => id;
            public string Url => url;
        }

        [Serializable]
        public struct DeveloperMessage
        {
            [SerializeField] private UnityEditor.MessageType type;
            [SerializeField] private List<ChangelogLink> links;
            [SerializeField] private List<LocalizedDeveloperMessageText> localizedTexts;

            public UnityEditor.MessageType Type => type;
            public List<ChangelogLink> Links => links;

            public bool HasText(DALanguage lang) => !string.IsNullOrEmpty(GetText(lang));

            public string GetText(DALanguage lang)
            {
                if (localizedTexts != null)
                {
                    for (int i = 0; i < localizedTexts.Count; i++)
                    {
                        if (string.Equals(localizedTexts[i].Language, lang.ToString(), StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(localizedTexts[i].Text))
                        {
                            return localizedTexts[i].Text;
                        }
                    }
                }
                return null;
            }
        }

        [Serializable]
        public struct LocalizedDeveloperMessageText
        {
            [SerializeField] private string language;
            [SerializeField] private string text;

            public string Language => language;
            public string Text => text;
        }
    }
}
