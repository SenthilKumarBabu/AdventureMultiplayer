#if UNITY_EDITOR
using DA_Assets.Constants;
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Shared.Extensions;
using DA_Assets.Singleton;
using DA_Assets.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.UCC
{
    public interface IConvConfig
    {
        string ProductVersion { get; }
        InternalLocalizator Localizator { get; }
        List<TagConfig> TagConfigs { get; }
        string DocsBaseUrl { get; }
        string WebLogFileName { get; }
        string ZipManifestFileName { get; }
        string ZipExtractFolderName { get; }
        string ZipNodeFileExtension { get; }
        string DefaultTmpShaderName { get; }
        string DateTimeFormat1 { get; }
        bool ShowDecorativeEditorBackground { get; set; }
        string CanvasGameObjectName { get; }
        string I2LocGameObjectName { get; }
        RoundingConfig Rounding { get; }
        int RecentProjectsLimit { get; }
        int LogFilesLimit { get; }
        int SetImgTypeSpriteMaxRetries { get; }
        int MaxRenderSize { get; }
        int SpriteDownloadTimeoutSeconds { get; }
        string BlurredObjectTag { get; }
        string BlurCameraTag { get; }
        char RealTagSeparator { get; }
        int ChildParsingLimit { get; }
        int ChunkSizeGetNodes { get; }
        int FrameListDepth { get; }
        string GoogleFontsApiKey { get; set; }
        ScriptableObject FuitkConfig { get; }
        ScriptableObject McpServerConfig { get; }
#if UNITY_EDITOR
        UnityEditor.MonoScript UitkConverterScript { get; }
#endif
        Sprite WhiteSprite32px { get; }
        Sprite MissingImageTexture128px { get; }
        TextAsset BaseClass { get; }
        Material ImageLinearMaterial { get; }
        VectorMaterials VectorMaterials { get; }
        GameObject HorizontalSpacePrefab { get; }
        GameObject VerticalSpacePrefab { get; }
        SerializedDictionary<ReasonKey, string> ReasonDescriptions { get; }
        float IMAGE_SCALE_MIN { get; }
        float IMAGE_SCALE_MAX { get; }
        char HierarchyDelimiter { get; }
        string PARENT_ID { get; }
        char AsterisksChar { get; }
        string RATEME_PREFS_KEY { get; }
        string RECENT_PROJECTS_PREFS_KEY { get; }
        string LogPath { get; }
        string CachePath { get; }
    }

    public abstract class ConvConfigBase<T> : AssetConfig<T>, IConvConfig where T : ConvConfigBase<T>
    {
        List<TagConfig> IConvConfig.TagConfigs => tags;
        string IConvConfig.DocsBaseUrl => docsBaseUrl;
        string IConvConfig.WebLogFileName => webLogFileName;
        string IConvConfig.ZipManifestFileName => zipManifestFileName;
        string IConvConfig.ZipExtractFolderName => zipExtractFolderName;
        string IConvConfig.ZipNodeFileExtension => zipNodeFileExtension;
        string IConvConfig.DefaultTmpShaderName => defaultTmpShaderName;
        string IConvConfig.DateTimeFormat1 => dateTimeFormat1;
        bool IConvConfig.ShowDecorativeEditorBackground
        {
            get => showDecorativeEditorBackground;
            set => showDecorativeEditorBackground = value;
        }
        string IConvConfig.CanvasGameObjectName => canvasGameObjectName;
        string IConvConfig.I2LocGameObjectName => i2LocGameObjectName;
        RoundingConfig IConvConfig.Rounding => rounding;
        int IConvConfig.RecentProjectsLimit => recentProjectsLimit;
        int IConvConfig.LogFilesLimit => logFilesLimit;
        int IConvConfig.SetImgTypeSpriteMaxRetries => setImgTypeSpriteMaxRetries;
        int IConvConfig.MaxRenderSize => maxRenderSize;
        int IConvConfig.SpriteDownloadTimeoutSeconds => spriteDownloadTimeoutSeconds;
        string IConvConfig.BlurredObjectTag => blurredObjectTag;
        string IConvConfig.BlurCameraTag => blurCameraTag;
        char IConvConfig.RealTagSeparator => realTagSeparator;
        int IConvConfig.ChildParsingLimit => childParsingLimit;
        int IConvConfig.ChunkSizeGetNodes => chunkSizeGetNodes;
        int IConvConfig.FrameListDepth => frameListDepth;
        string IConvConfig.GoogleFontsApiKey { get => GoogleFontsApiKey; set => GoogleFontsApiKey = value; }
        ScriptableObject IConvConfig.FuitkConfig => fuitkConfig;
        ScriptableObject IConvConfig.McpServerConfig => mcpServerConfig;
        Sprite IConvConfig.WhiteSprite32px => whiteSprite32px;
        Sprite IConvConfig.MissingImageTexture128px => missingImageTexture128px;
        TextAsset IConvConfig.BaseClass => baseClass;
        Material IConvConfig.ImageLinearMaterial => imageLinearMaterial;
        VectorMaterials IConvConfig.VectorMaterials => vectorMaterials;
        GameObject IConvConfig.HorizontalSpacePrefab => horizontalSpacePrefab;
        GameObject IConvConfig.VerticalSpacePrefab => verticalSpacePrefab;
        SerializedDictionary<ReasonKey, string> IConvConfig.ReasonDescriptions => reasonDescriptions;
        float IConvConfig.IMAGE_SCALE_MIN => IMAGE_SCALE_MIN;
        float IConvConfig.IMAGE_SCALE_MAX => IMAGE_SCALE_MAX;
        char IConvConfig.HierarchyDelimiter => hierarchyDelimiter;
        string IConvConfig.PARENT_ID => parentId;
        char IConvConfig.AsterisksChar => asterisksChar;
        string IConvConfig.RATEME_PREFS_KEY => rateMePrefsKey;
        string IConvConfig.RECENT_PROJECTS_PREFS_KEY => recentProjectsPrefsKey;
        string IConvConfig.LogPath => LogPath;
        string IConvConfig.CachePath => CachePath;

        [SerializeField] List<TagConfig> tags;
        public static List<TagConfig> TagConfigs => Instance.tags;

        [Header("Docs Getter")]
        [SerializeField] string docsBaseUrl = "https://da-assets.gitbook.io/docs";
        public static string DocsBaseUrl => Instance.docsBaseUrl;

        [Header("File names")]
        [SerializeField] string webLogFileName;
        public static string WebLogFileName => Instance.webLogFileName;

        [SerializeField] string zipManifestFileName = "manifest.json";
        public static string ZipManifestFileName => Instance.zipManifestFileName;

        [SerializeField] string zipExtractFolderName = "FCU_Zip_Import";
        public static string ZipExtractFolderName => Instance.zipExtractFolderName;

        [SerializeField] string zipNodeFileExtension = ".json";
        public static string ZipNodeFileExtension => Instance.zipNodeFileExtension;

        [SerializeField] string defaultTmpShaderName = "TextMeshPro/Distance Field";
        public static string DefaultTmpShaderName => Instance.defaultTmpShaderName;

        [Header("Formats")]
        [SerializeField] string dateTimeFormat1;
        public static string DateTimeFormat1 => Instance.dateTimeFormat1;

        [Header("Editor UI")]
        [Tooltip("Shows the decorative background in the FCU inspector: top banner, noise overlay, and background image.")]
        [SerializeField] bool showDecorativeEditorBackground = true;
        public static bool ShowDecorativeEditorBackground
        {
            get => Instance.showDecorativeEditorBackground;
            set => Instance.showDecorativeEditorBackground = value;
        }

        [Header("GameObject names")]
        [SerializeField] string canvasGameObjectName;
        public static string CanvasGameObjectName => Instance.canvasGameObjectName;

        [SerializeField] string i2LocGameObjectName;
        public static string I2LocGameObjectName => Instance.i2LocGameObjectName;

        [Header("Values")]

        [SerializeField] RoundingConfig rounding;
        public static RoundingConfig Rounding => Instance.rounding;

        [SerializeField] int recentProjectsLimit = 20;
        public static int RecentProjectsLimit => Instance.recentProjectsLimit;

        [SerializeField] int logFilesLimit = 50;
        public static int LogFilesLimit => Instance.logFilesLimit;

        [Tooltip("Hard cap on retries inside SpriteProcessor.SetImgTypeSprite. Each iteration awaits 100 ms; this budget exists purely as a safety net so the import never hangs on a stuck importer. 50 * 100 ms = 5 s.")]
        [SerializeField] int setImgTypeSpriteMaxRetries = 50;
        public static int SetImgTypeSpriteMaxRetries => Instance.setImgTypeSpriteMaxRetries;

        [SerializeField] int maxRenderSize = 4096;
        public static int MaxRenderSize => Instance.maxRenderSize;

        [SerializeField] int spriteDownloadTimeoutSeconds = 180;
        public static int SpriteDownloadTimeoutSeconds => Instance.spriteDownloadTimeoutSeconds;

        [SerializeField] string blurredObjectTag = "UIBlur";
        public static string BlurredObjectTag => Instance.blurredObjectTag;

        [SerializeField] string blurCameraTag = "BackgroundBlur";
        public static string BlurCameraTag => Instance.blurCameraTag;

        [SerializeField] char realTagSeparator = '-';
        public static char RealTagSeparator => Instance.realTagSeparator;

        [Tooltip("If an object has more than **N** children, **SmartTags** and **Hashes** will not be assigned to them.")]
        [SerializeField] int childParsingLimit = 512;
        public static int ChildParsingLimit => Instance.childParsingLimit;

        [Header("Api")]

        [SerializeField] int chunkSizeGetNodes;
        public static int ChunkSizeGetNodes => Instance.chunkSizeGetNodes;

        [SerializeField] int frameListDepth = 2;
        public static int FrameListDepth => Instance.frameListDepth;

        [SerializeField] string googleFontsApiKeyPrefsKey = "FCU_GOOGLE_FONTS_API_KEY";
        public static string GOOGLE_FONTS_API_KEY_PREFS_KEY => Instance.googleFontsApiKeyPrefsKey;

        public static string GoogleFontsApiKey
        {
            get
            {
#if UNITY_EDITOR
                return EditorPrefs.GetString(GOOGLE_FONTS_API_KEY_PREFS_KEY, "");
#else
                return "";
#endif
            }
            set
            {
#if UNITY_EDITOR
                EditorPrefs.SetString(GOOGLE_FONTS_API_KEY_PREFS_KEY, value ?? "");
#endif
            }
        }

        [Header("Other")]

        [SerializeField] ScriptableObject fuitkConfig;
        public static ScriptableObject FuitkConfig => Instance.fuitkConfig;

        [SerializeField] ScriptableObject mcpServerConfig;
        public static ScriptableObject McpServerConfig => Instance.mcpServerConfig;

#if UNITY_EDITOR
        [SerializeField] UnityEditor.MonoScript uitkConverterScript;
        public static UnityEditor.MonoScript UitkConverterScript => Instance.uitkConverterScript;
        UnityEditor.MonoScript IConvConfig.UitkConverterScript => uitkConverterScript;
#endif

        [SerializeField] Sprite whiteSprite32px;
        public static Sprite WhiteSprite32px => Instance.whiteSprite32px;

        [SerializeField] Sprite missingImageTexture128px;
        public static Sprite MissingImageTexture128px => Instance.missingImageTexture128px;

        [SerializeField] TextAsset baseClass;
        public static TextAsset BaseClass => Instance.baseClass;

        [SerializeField] Material imageLinearMaterial;
        public static Material ImageLinearMaterial => Instance.imageLinearMaterial;

        [SerializeField] VectorMaterials vectorMaterials;
        public static VectorMaterials VectorMaterials => Instance.vectorMaterials;

        [Header("Space Between Prefabs")]
        [SerializeField] GameObject horizontalSpacePrefab;
        public static GameObject HorizontalSpacePrefab => Instance.horizontalSpacePrefab;

        [SerializeField] GameObject verticalSpacePrefab;
        public static GameObject VerticalSpacePrefab => Instance.verticalSpacePrefab;

        [Header("Reason Descriptions")]

        [SerializeField] SerializedDictionary<ReasonKey, string> reasonDescriptions = new();
        public static SerializedDictionary<ReasonKey, string> ReasonDescriptions => Instance.reasonDescriptions;

        public static float IMAGE_SCALE_MIN => 0.25f;
        public static float IMAGE_SCALE_MAX => 4f;

        [SerializeField] char hierarchyDelimiter = '/';
        public static char HierarchyDelimiter => Instance.hierarchyDelimiter;

        [SerializeField] string parentId = "603951929:602259738";
        public static string PARENT_ID => Instance.parentId;

        [SerializeField] char asterisksChar = '•';
        public static char AsterisksChar => Instance.asterisksChar;

        public static string DefaultLocalizationCulture => "en-US";

        [SerializeField] string rateMePrefsKey = "DONT_SHOW_RATEME";
        public static string RATEME_PREFS_KEY => Instance.rateMePrefsKey;

        [SerializeField] string recentProjectsPrefsKey = "recentProjectsPrefsKey";
        public static string RECENT_PROJECTS_PREFS_KEY => Instance.recentProjectsPrefsKey;

        private static string logPath;
        public static string LogPath
        {
            get
            {
                if (logPath.IsEmpty())
                    logPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");

                logPath.CreateFolderIfNotExists();

                return logPath;
            }
        }

        private static string cachePath;
        public static string CachePath
        {
            get
            {
                if (cachePath.IsEmpty())
                {
                    string tempFolder = Path.GetTempPath();
                    cachePath = Path.Combine(tempFolder, "FcuCache");
                }

                cachePath.CreateFolderIfNotExists();

                return cachePath;
            }
        }
    }
}
#endif