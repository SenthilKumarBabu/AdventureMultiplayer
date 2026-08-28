#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.Logging;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class MainSettings : FcuBase
    {
        [SerializeField] ImportMode importMode = ImportMode.Url;
        public ImportMode ImportMode
        {
            get => monoBeh?.ForcedImportMode ?? importMode;
            set => importMode = monoBeh?.ForcedImportMode ?? value;
        }

        [SerializeField] string zipArchivePath;
        public string ZipArchivePath { get => zipArchivePath; set => zipArchivePath = value; }

        [SerializeField] UIFramework uiFramework = UIFramework.UGUI;
        public UIFramework UIFramework
        {
            get => uiFramework;
            set => uiFramework = value;
        }

        [SerializeField] PositioningMode positioningMode = PositioningMode.Absolute;
        public PositioningMode PositioningMode
        {
            get => positioningMode;
            set
            {
                if (value == PositioningMode.GameView && uiFramework != UIFramework.UGUI)
                {
                    Debug.LogError(FcuLocKey.log_main_settings_positioning_not_supported.Localize(value, uiFramework));
                    value = PositioningMode.Absolute;
                }

                positioningMode = value;
            }
        }

        [SerializeField] PivotType pivotType = PivotType.MiddleCenter;
        public PivotType PivotType { get => pivotType; set => pivotType = value; }

        [SerializeField] int goLayer = 5;
        public int GameObjectLayer { get => goLayer; set => goLayer = value; }

        [SerializeField] bool useDuplicateFinder = true;
        public bool UseDuplicateFinder { get => useDuplicateFinder; set => useDuplicateFinder = value; }

        [SerializeField] bool useLayoutUpdater = false;
        public bool UseLayoutUpdater
        {
            get => useLayoutUpdater;
            set
            {
                if (value)
                {
                    Debug.LogError(FcuLocKey.log_layout_updater_temporarily_not_supported.Localize());
                    useLayoutUpdater = false;
                    return;
                }

                useLayoutUpdater = false;
            }
        }

        [SerializeField] bool destroySyncHelpersAfterImport = false;
        public bool DestroySyncHelpersAfterImport { get => destroySyncHelpersAfterImport; set => destroySyncHelpersAfterImport = value; }

        public bool ShouldDestroySyncHelpersAfterImport()
        {
            return destroySyncHelpersAfterImport && !monoBeh.IsDebug();
        }

        [SerializeField] bool rawImport = false;
        public bool RawImport
        {
            get => rawImport;
            set
            {
                if (value && value != rawImport)
                {
                    Debug.LogError(FcuLocKey.log_dev_function_enabled.Localize(FcuLocKey.label_raw_import.Localize()));
                }

                rawImport = value;
            }
        }

        [SerializeField] bool https = true;
        public bool Https { get => https; set => https = value; }

        [SerializeField] FigmaEnvironment figmaEnvironment = FigmaEnvironment.Figma;
        public FigmaEnvironment FigmaEnvironment { get => figmaEnvironment; set => figmaEnvironment = value; }

        [SerializeField] int gameObjectNameMaxLength = 32;
        public int GameObjectNameMaxLenght { get => gameObjectNameMaxLength; set => gameObjectNameMaxLength = value; }

        [SerializeField] int textObjectNameMaxLength = 16;
        public int TextObjectNameMaxLenght { get => textObjectNameMaxLength; set => textObjectNameMaxLength = value; }

        [SerializeField] bool windowMode = false;
        public bool WindowMode { get => windowMode; set => windowMode = value; }

        [SerializeField] bool drawLayoutGrids = false;
        public bool DrawLayoutGrids { get => drawLayoutGrids; set => drawLayoutGrids = value; }

        [SerializeField] string projectUrl;
        public string ProjectUrl
        {
            get => projectUrl;
            set
            {
                projectUrl = value;

                if (TryGetFigmaFileId(projectUrl, out string fileId))
                {
                    projectId = fileId;
                }
                else
                {
                    projectId = "";
                }
            }
        }

        [SerializeField] string projectId;
        public string ProjectId { get => projectId; set => projectId = value; }

        public bool TryGetFigmaFileId(string input, out string fileId)
        {
            fileId = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string idPattern = @"^[0-9a-zA-Z]{22,128}$";
            if (Regex.IsMatch(input, idPattern))
            {
                fileId = input;
                return true;
            }

            string urlPattern = @"^(?:https?:\/\/)?(?:[\w\.-]+\.)?figma\.com\/([\w-]+)\/([0-9a-zA-Z]{22,128})(?:\/[^?]*)?(?:\?.*)?$";
            var match = Regex.Match(input, urlPattern);

            if (match.Success)
            {
                fileId = match.Groups[2].Value;
                return true;
            }

            return false;
        }
    }
}
#endif