using DA_Assets.DAI;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal partial class TextFontsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private void DrawPathSettings(VisualElement parent)
        {
            SerializedProperty fontLoaderProp = scriptableObject.SerializedObject.FindProperty(nameof(ConverterBase.FontLoader));

            if (fontLoaderProp == null)
            {
                VisualElement errorPanel = uitk.CreateSectionPanel();
                errorPanel.Add(uitk.HelpBox(new HelpBoxData
                {
                    Message = scriptableObject.Localize(FcuLocKey.textfonts_error_fontloader_not_found, nameof(ConverterBase.FontLoader)),
                    MessageType = MessageType.Error
                }));
                parent.Add(errorPanel);
                return;
            }

            DrawTtfFontSection(parent, fontLoaderProp);
            parent.Add(uitk.Space10());

            DrawUitkFontSection(parent, fontLoaderProp);

#if TextMeshPro
            parent.Add(uitk.Space10());
            DrawTmpFontSection(parent, fontLoaderProp);
#endif

#if UNITEXT
            parent.Add(uitk.Space10());
            DrawUniTextFontSection(parent, fontLoaderProp);
#endif
        }

        private void DrawTtfFontSection(VisualElement parent, SerializedProperty fontLoaderProp)
        {
            VisualElement panel = CreateFontPanel(parent, "TTF FONTS", scriptableObject.Localize(FcuLocKey.tooltip_ttf_path));

            var ttfPathContainer = uitk.CreateFolderInput(
                label: scriptableObject.Localize(FcuLocKey.label_ttf_path),
                tooltip: scriptableObject.Localize(FcuLocKey.tooltip_ttf_path),
                initialValue: monoBeh.FontLoader.TtfFontsPath,
                onPathChanged: newValue => monoBeh.FontLoader.TtfFontsPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    scriptableObject.Localize(FcuLocKey.label_select_fonts_folder),
                    monoBeh.FontLoader.TtfFontsPath,
                    ""),
                buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_fonts_folder));
            ttfPathContainer.AddFolderResetMenu(
                () => monoBeh.FontLoader.TtfFontsPath,
                "",
                v => monoBeh.FontLoader.TtfFontsPath = v);

            panel.Add(ttfPathContainer);
            panel.Add(uitk.ItemSeparator());

            var addTtfButton = uitk.Button(scriptableObject.Localize(FcuLocKey.label_add_ttf_fonts_from_folder), () =>
            {
                monoBeh.FontDownloader.AddTtfFontsCts?.Cancel();
                monoBeh.FontDownloader.AddTtfFontsCts?.Dispose();
                monoBeh.FontDownloader.AddTtfFontsCts = new CancellationTokenSource();
                _ = monoBeh.FontLoader.AddToTtfFontsList(monoBeh.FontDownloader.AddTtfFontsCts.Token);
            });
            addTtfButton.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_add_ttf_fonts_from_folder);
            panel.Add(addTtfButton);
            panel.Add(uitk.ItemSeparator());

            panel.Add(CreateBoundPropertyField(fontLoaderProp, nameof(FontLoader.TtfFonts)));
        }

        private void DrawUitkFontSection(VisualElement parent, SerializedProperty fontLoaderProp)
        {
            VisualElement panel = CreateFontPanel(parent, "UITK FONTASSETS", "UI Toolkit TextCore FontAsset settings.");

            var uitkPathContainer = uitk.CreateFolderInput(
                label: "UITK FontAsset Path",
                tooltip: "Folder that stores UI Toolkit TextCore FontAsset assets.",
                initialValue: monoBeh.FontLoader.UitkFontAssetsPath,
                onPathChanged: newValue => monoBeh.FontLoader.UitkFontAssetsPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    scriptableObject.Localize(FcuLocKey.label_select_fonts_folder),
                    monoBeh.FontLoader.UitkFontAssetsPath,
                    ""),
                buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_fonts_folder));
            uitkPathContainer.AddFolderResetMenu(
                () => monoBeh.FontLoader.UitkFontAssetsPath,
                "",
                v => monoBeh.FontLoader.UitkFontAssetsPath = v);

            panel.Add(uitkPathContainer);
            panel.Add(uitk.ItemSeparator());

            var buttonsRow = new VisualElement();
            buttonsRow.style.flexDirection = FlexDirection.Row;

            var addUitkButton = uitk.Button("Add UITK FontAssets from Folder", () =>
            {
                monoBeh.FontDownloader.AddUitkFontAssetsCts?.Cancel();
                monoBeh.FontDownloader.AddUitkFontAssetsCts?.Dispose();
                monoBeh.FontDownloader.AddUitkFontAssetsCts = new CancellationTokenSource();
                _ = monoBeh.FontLoader.AddToUitkFontAssetsList(monoBeh.FontDownloader.AddUitkFontAssetsCts.Token);
            });
            addUitkButton.tooltip = "Scan the configured folder and refresh the UITK FontAsset registry.";
            addUitkButton.style.flexGrow = 1;

            var createUitkButton = uitk.Button("Create UITK FontAssets from TTF", () =>
            {
                var cts = new CancellationTokenSource();
                _ = monoBeh.FontDownloader.UitkFontAssetCreator.CreateFromTtfFolder(cts.Token);
            });
            createUitkButton.tooltip = "Create TextCore FontAsset assets from the configured TTF folder.";
            createUitkButton.style.flexGrow = 1;

            buttonsRow.Add(addUitkButton);
            buttonsRow.Add(uitk.Space5());
            buttonsRow.Add(createUitkButton);
            panel.Add(buttonsRow);
            panel.Add(uitk.ItemSeparator());

            panel.Add(CreateBoundPropertyField(fontLoaderProp, nameof(FontLoader.UitkFontAssets)));
        }

#if TextMeshPro
        private void DrawTmpFontSection(VisualElement parent, SerializedProperty fontLoaderProp)
        {
            VisualElement panel = CreateFontPanel(parent, "TMP FONTS", scriptableObject.Localize(FcuLocKey.tooltip_tmp_path));

            var tmpPathContainer = uitk.CreateFolderInput(
                label: scriptableObject.Localize(FcuLocKey.label_tmp_path),
                tooltip: scriptableObject.Localize(FcuLocKey.tooltip_tmp_path),
                initialValue: monoBeh.FontLoader.TmpFontsPath,
                onPathChanged: newValue => monoBeh.FontLoader.TmpFontsPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    scriptableObject.Localize(FcuLocKey.label_select_fonts_folder),
                    monoBeh.FontLoader.TmpFontsPath,
                    ""),
                buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_fonts_folder));
            tmpPathContainer.AddFolderResetMenu(
                () => monoBeh.FontLoader.TmpFontsPath,
                "",
                v => monoBeh.FontLoader.TmpFontsPath = v);

            panel.Add(tmpPathContainer);
            panel.Add(uitk.ItemSeparator());

            var buttonsRow = new VisualElement();
            buttonsRow.style.flexDirection = FlexDirection.Row;

            var addTmpButton = uitk.Button(scriptableObject.Localize(FcuLocKey.label_add_tmp_fonts_from_folder), () =>
            {
                monoBeh.FontDownloader.AddTmpFontsCts?.Cancel();
                monoBeh.FontDownloader.AddTmpFontsCts?.Dispose();
                monoBeh.FontDownloader.AddTmpFontsCts = new CancellationTokenSource();
                _ = monoBeh.FontLoader.AddToTmpMeshFontsList(monoBeh.FontDownloader.AddTmpFontsCts.Token);
            });
            addTmpButton.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_add_fonts_from_folder);
            addTmpButton.style.flexGrow = 1;

            var createTmpButton = uitk.Button(scriptableObject.Localize(FcuLocKey.label_create_tmp_from_ttf), () =>
            {
                var cts = new CancellationTokenSource();
                _ = monoBeh.FontDownloader.TmpDownloader.CreateFromTtfFolder(cts.Token);
            });
            createTmpButton.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_create_tmp_from_ttf);
            createTmpButton.style.flexGrow = 1;

            buttonsRow.Add(addTmpButton);
            buttonsRow.Add(uitk.Space5());
            buttonsRow.Add(createTmpButton);
            panel.Add(buttonsRow);
            panel.Add(uitk.ItemSeparator());

            panel.Add(CreateBoundPropertyField(fontLoaderProp, nameof(FontLoader.TmpFonts)));
        }
#endif

#if UNITEXT
        private void DrawUniTextFontSection(VisualElement parent, SerializedProperty fontLoaderProp)
        {
            VisualElement panel = CreateFontPanel(parent, "UNITEXT", scriptableObject.Localize(FcuLocKey.tooltip_unitext_fonts_path));

            var uniTextPathContainer = uitk.CreateFolderInput(
                label: scriptableObject.Localize(FcuLocKey.label_unitext_fonts_path),
                tooltip: scriptableObject.Localize(FcuLocKey.tooltip_unitext_fonts_path),
                initialValue: monoBeh.FontLoader.UniTextFontsPath,
                onPathChanged: newValue => monoBeh.FontLoader.UniTextFontsPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    scriptableObject.Localize(FcuLocKey.label_select_fonts_folder),
                    monoBeh.FontLoader.UniTextFontsPath,
                    ""),
                buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_fonts_folder));
            uniTextPathContainer.AddFolderResetMenu(
                () => monoBeh.FontLoader.UniTextFontsPath,
                "",
                v => monoBeh.FontLoader.UniTextFontsPath = v);

            panel.Add(uniTextPathContainer);
            panel.Add(uitk.ItemSeparator());

            var buttonsRow = new VisualElement();
            buttonsRow.style.flexDirection = FlexDirection.Row;

            var addUniTextButton = uitk.Button(scriptableObject.Localize(FcuLocKey.label_add_unitext_fonts_from_folder), () =>
            {
                var cts = new CancellationTokenSource();
                _ = monoBeh.FontLoader.AddToUniTextFontsList(cts.Token);
            });
            addUniTextButton.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_add_unitext_fonts_from_folder);
            addUniTextButton.style.flexGrow = 1;

            var createUniTextButton = uitk.Button(scriptableObject.Localize(FcuLocKey.label_create_unitext_from_ttf), () =>
            {
                var cts = new CancellationTokenSource();
                _ = monoBeh.FontDownloader.UniTextFontCreator.CreateFromTtfFolder(cts.Token);
            });
            createUniTextButton.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_create_unitext_from_ttf);
            createUniTextButton.style.flexGrow = 1;

            buttonsRow.Add(addUniTextButton);
            buttonsRow.Add(uitk.Space5());
            buttonsRow.Add(createUniTextButton);
            panel.Add(buttonsRow);
            panel.Add(uitk.ItemSeparator());

            panel.Add(CreateBoundPropertyField(fontLoaderProp, nameof(FontLoader.UniTextFontStacks)));
        }
#endif

        private VisualElement CreateFontPanel(VisualElement parent, string title, string tooltip)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(title);
            header.tooltip = tooltip;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            return panel;
        }

        private PropertyField CreateBoundPropertyField(SerializedProperty parentProperty, string propertyName)
        {
            SerializedProperty property = parentProperty.FindPropertyRelative(propertyName);
            var field = new PropertyField(property);
            field.Bind(scriptableObject.SerializedObject);
            return field;
        }
    }
}