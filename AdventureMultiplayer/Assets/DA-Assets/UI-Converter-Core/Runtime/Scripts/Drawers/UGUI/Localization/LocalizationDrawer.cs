#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Threading;

#pragma warning disable CS0649
#pragma warning disable IDE0003

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class LocalizationDrawer : FcuBase
    {
        private Dictionary<string, string> _localizationDictionary = new Dictionary<string, string>();
        public Dictionary<string, string> LocalizationDictionary => _localizationDictionary;

        public override void Init(ConverterBase monoBeh)
        {
            base.Init(monoBeh);
            this.DALocalizatorDrawer.Init(monoBeh);
#if I2LOC_EXISTS && UNITY_EDITOR
            this.I2LocalizationDrawer.Init(monoBeh);
#endif
        }

        public void ClearLocalization()
        {
            _localizationDictionary.Clear();

            switch (monoBeh.Settings.LocalizationSettings.LocalizationComponent)
            {
                case LocalizationComponent.DALocalizator:
                    break;
                case LocalizationComponent.I2Localization:
#if I2LOC_EXISTS && UNITY_EDITOR
                    this.I2LocalizationDrawer.Init();
#endif
                    break;
            }
        }

        public void Draw(Node fobject)
        {

            string locKey = fobject.Data.Names.LocKey;

            if (locKey.IsEmpty())
                return;

            string text = fobject.GetText();

            if (text.IsEmpty())
                return;

            _localizationDictionary.TryAddValue(locKey, text);

            switch (monoBeh.Settings.LocalizationSettings.LocalizationComponent)
            {
                case LocalizationComponent.DALocalizator:
                    this.DALocalizatorDrawer.Draw(locKey, fobject);
                    break;
                case LocalizationComponent.I2Localization:
#if I2LOC_EXISTS && UNITY_EDITOR
                    this.I2LocalizationDrawer.Draw(locKey, fobject);
#endif
                    break;
            }
        }

        public string SaveTable(CancellationToken token)
        {
            string folderPath = monoBeh.Settings.LocalizationSettings.LocFolderPath;
            string fileName = monoBeh.Settings.LocalizationSettings.LocFileName;

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);
            char separator = (char)monoBeh.Settings.LocalizationSettings.CsvSeparator;

            token.ThrowIfCancellationRequested();

            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(false)))
            {
                writer.Write(EscapeCsvValue("Key", separator));
                writer.Write(separator);
                writer.WriteLine(EscapeCsvValue(monoBeh.Settings.LocalizationSettings.CurrentFigmaLayoutCulture, separator));

                foreach (var kvp in _localizationDictionary)
                {
                    token.ThrowIfCancellationRequested();
                    writer.Write(EscapeCsvValue(kvp.Key, separator));
                    writer.Write(separator);
                    writer.WriteLine(EscapeCsvValue(kvp.Value, separator));
                }
            }

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            return filePath.RemovePathExtension();
        }

        private static string EscapeCsvValue(string value, char separator)
        {
            if (value == null)
            {
                return "";
            }

            bool quote = value.IndexOf(separator) >= 0 ||
                         value.IndexOf('"') >= 0 ||
                         value.IndexOf('\r') >= 0 ||
                         value.IndexOf('\n') >= 0;

            if (!quote)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static string RemoveAssetsAndResourcesFolders(string path)
        {
            string normalizedPath = path.Replace('\\', Path.DirectorySeparatorChar)
                                        .Replace('/', Path.DirectorySeparatorChar);

            string assetsResourcesPrefix = $"Assets{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}";
            string assetsPrefix = $"Assets{Path.DirectorySeparatorChar}";

            if (normalizedPath.StartsWith(assetsResourcesPrefix))
            {
                return normalizedPath.Substring(assetsResourcesPrefix.Length);
            }
            else if (normalizedPath.StartsWith(assetsPrefix))
            {
                return normalizedPath.Substring(assetsPrefix.Length);
            }
            else
            {
                return normalizedPath;
            }
        }

        public void SaveAndConnectTable(CancellationToken token)
        {
            string filePath = SaveTable(token);
            Debug.Log(FcuLocKey.log_localization_file_saved.Localize(filePath));
            ConnectTable(filePath, token);
        }

        internal void ConnectTable(string filePath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            switch (monoBeh.Settings.LocalizationSettings.LocalizationComponent)
            {
                case LocalizationComponent.DALocalizator:
                    {
                        this.DALocalizatorDrawer.ConnectTable(filePath);
                    }
                    break;
                case LocalizationComponent.I2Localization:
                    {
#if I2LOC_EXISTS && UNITY_EDITOR
                        this.I2LocalizationDrawer.ConnectTable(filePath);
#endif
                    }
                    break;
            }
        }

#if I2LOC_EXISTS && UNITY_EDITOR
        [SerializeField] public I2LocalizationDrawer I2LocalizationDrawer = new I2LocalizationDrawer();
#endif
        [SerializeField] public DALocalizatorDrawer DALocalizatorDrawer = new DALocalizatorDrawer();
    }
}
#endif