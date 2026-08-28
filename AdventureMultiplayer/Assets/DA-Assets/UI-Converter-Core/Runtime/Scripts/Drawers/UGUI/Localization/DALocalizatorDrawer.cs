#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using System;
using UnityEngine;
using Resources = UnityEngine.Resources;

#if DALOC_EXISTS
using DA_Assets.DAL;
#endif

#pragma warning disable CS0649

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class DALocalizatorDrawer : FcuBase
    {
        public void ConnectTable(string filePath)
        {
            if (monoBeh.Settings.LocalizationSettings.Localizator == null)
            {
                Debug.LogError(FcuLocKey.log_localization_provider_null.Localize());
                return;
            }

            TextAsset localizationFile = Resources.Load<TextAsset>(filePath);

            if (localizationFile != null)
            {
#if DALOC_EXISTS
                ILocalizator localizator = monoBeh.Settings.LocalizationSettings.Localizator as ILocalizator;
#endif
            }
            else
            {
                Debug.LogError(FcuLocKey.log_localization_file_not_loaded.Localize(filePath));
            }
        }

        public void Draw(string locKey, Node fobject)
        {
#if DALOC_EXISTS
            if (monoBeh.UsingTextMesh())
            {
                fobject.Data.GameObject.TryAddComponent(out TextMeshLocalizator tmpText);
                tmpText.Key = locKey;
            }
            else if (monoBeh.UsingUnityText())
            {
                fobject.Data.GameObject.TryAddComponent(out UITextLocalizator uiTextLoc);
                uiTextLoc.Key = locKey;
            }
#endif
        }
    }
}
#endif