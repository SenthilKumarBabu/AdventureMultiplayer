#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class AssetTools : FcuBase
    {
        public void CancelToken()
        {
            monoBeh.ProjectImporter.ImportTokenSource?.Cancel();
            monoBeh.ProjectImporter.ImportTokenSource = null;
        }

        public ImportResult StopAsset(ImportStatus status, Exception ex = null)
        {
            CancelToken();
            monoBeh.EditorDelegateHolder.StopAllProgress?.Invoke(monoBeh);

            if (status == ImportStatus.Technical)
            {
                return ImportResult.Return(ImportStatus.Technical, string.Empty, -1);
            }

            string message = status switch
            {
                ImportStatus.Stopped => FcuLocKey.log_import_stoped_manually.Localize(),
                ImportStatus.CantAuthorize => FcuLocKey.log_cant_auth.Localize(),
                ImportStatus.ImportSuccess => FcuLocKey.log_import_complete.Localize(),
                ImportStatus.ProjectDownloadSuccess => FcuLocKey.log_project_downloaded.Localize(),
                _ => FcuLocKey.log_import_stoped_because_error.Localize()
            };

            if (status == ImportStatus.ImportSuccess)
            {
                DALogger.LogSuccess(message);
                monoBeh.Events.OnImportComplete?.Invoke(monoBeh);
                return ImportResult.Return(status, message, -1);
            }
            else if (status == ImportStatus.ProjectDownloadSuccess)
            {
                DALogger.LogSuccess(message);
                monoBeh.Events.OnProjectDownloaded?.Invoke(monoBeh);
                return ImportResult.Return(status, message, -1);
            }

            if (ex != null)
            {
                Debug.LogException(ex);
            }

            Debug.LogError(message);

            monoBeh.Events.OnImportFail?.Invoke(monoBeh);
            return ImportResult.Return(status, message, -1);
        }

        public async Task ReselectFcu(CancellationToken token)
        {
            GameObject tempGo = MonoBehExtensions.CreateEmptyGameObject();
            await Task.Delay(100, token);
            tempGo.MakeGameObjectSelectedInHierarchy();
            await Task.Delay(100, token);
            SelectFcu();
            tempGo.Destroy();
        }

        public void SelectFcu()
        {
            monoBeh.gameObject.MakeGameObjectSelectedInHierarchy();
        }

        [HideInInspector, SerializeField] bool needShowRateMe;
        public bool NeedShowRateMe
        {
            get
            {
                if (needShowRateMe)
                {
#if UNITY_EDITOR
                    if (UnityEditor.EditorPrefs.GetInt(monoBeh.Config.RATEME_PREFS_KEY, 0) == 1)
                        return false;
#else
                    return false;
#endif
                }

                return needShowRateMe;
            }
            set => needShowRateMe = value;
        }

        public static T CreateConverterOnScene<T>() where T : ConverterBase
        {
            GameObject go = MonoBehExtensions.CreateEmptyGameObject();

            T converter = go.AddComponent<T>();
            go.name = string.Format(converter.Config.CanvasGameObjectName, converter.Guid);

            converter.CanvasDrawer.AddCanvasComponent();
            return converter;
        }

        internal void ShowRateMe()
        {
            int componentsCount = monoBeh.TagSetter.TagsCounter.Values.Sum();
            int importErrorCount = monoBeh.AssetTools.GetConsoleErrorCount();

            if (importErrorCount > 0 || componentsCount < 1)
            {
                needShowRateMe = false;
                return;
            }

            needShowRateMe = true;
        }

        public int GetConsoleErrorCount()
        {
#if UNITY_EDITOR
            try
            {
                Type logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
                if (logEntriesType == null)
                {
                    return 0;
                }

                MethodInfo getCountsByTypeMethod = logEntriesType.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public);
                if (getCountsByTypeMethod == null)
                {
                    return 0;
                }

                int errorCount = 0;
                int warningCount = 0;
                int logCount = 0;
                object[] args = new object[] { errorCount, warningCount, logCount };

                getCountsByTypeMethod.Invoke(null, args);

                errorCount = (int)args[0];
                warningCount = (int)args[1];
                logCount = (int)args[2];

                return errorCount;
            }
            catch (Exception)
            {
                return 1;
            }
#else
            return 1;
#endif
        }

        public static int GetMaxFileNumber(string folderPath, string prefix, string extension)
        {
            string[] files = Directory.GetFiles(folderPath, $"{prefix}*.{extension}", SearchOption.AllDirectories);
            int maxNumber = -1;

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                int number = ExtractFileNumber(fileName, prefix);
                if (number > maxNumber)
                {
                    maxNumber = number;
                }
            }

            return maxNumber;
        }

        private static int ExtractFileNumber(string fileName, string prefix)
        {
            if (fileName == prefix)
            {
                return 0;
            }

            char[] separators = { ' ', '-', '_' };

            foreach (char separator in separators)
            {
                if (fileName.StartsWith(prefix + separator))
                {
                    string numberPart = fileName.Substring(prefix.Length + 1);
                    if (int.TryParse(numberPart, out int number))
                    {
                        return number;
                    }
                }
            }

            return -1;
        }
    }
}
#endif