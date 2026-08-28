using DA_Assets.Constants;
using DA_Assets.DAI;
using DA_Assets.UCC.Extensions;
using DA_Assets.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DA_Assets.UCC.Model;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace DA_Assets.UCC
{
    internal class ImageSpritesTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private VisualElement dynamicSettingsContainer;

        public VisualElement Draw()
        {
            VisualElement root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            var titleEl = uitk.CreateTitle(
                scriptableObject.Localize(FcuLocKey.label_images_and_sprites_tab),
                scriptableObject.Localize(FcuLocKey.tooltip_images_and_sprites_tab)
            );
            titleEl.AddSectionResetMenu(() =>
            {
                var d = FcuDefaults.ImageSpritesSettings;
                var s = monoBeh.Settings.ImageSpritesSettings;
                s.ImageComponent = d.ImageComponent;
                s.ImageFormat = d.ImageFormat;
                s.ImageScale = d.ImageScale;
                s.MaxSpriteSize = d.MaxSpriteSize;
                s.PixelsPerUnit = d.PixelsPerUnit;
                s.RedownloadSprites = d.RedownloadSprites;
                s.DownloadOptions = d.DownloadOptions;
                s.PreserveRatioMode = d.PreserveRatioMode;
                s.UseImageLinearMaterial = d.UseImageLinearMaterial;
                scriptableObject.RefreshTabs();
            });
            root.Add(titleEl);
            root.Add(uitk.Space10());

            string pathToImageSpritesSettings = $"{nameof(monoBeh.Settings)}.{nameof(monoBeh.Settings.ImageSpritesSettings)}";
            SerializedProperty imageSpritesSettingsProp = scriptableObject.SerializedObject.FindProperty(pathToImageSpritesSettings);

            if (imageSpritesSettingsProp == null)
            {
                root.Add(new HelpBox(scriptableObject.Localize(FcuLocKey.imagesprites_error_settings_not_found), HelpBoxMessageType.Error));
                return root;
            }

            DrawImageComponentPanel(root);
            root.Add(uitk.Space10());

            DrawGeneralSettings(root, imageSpritesSettingsProp);
            root.Add(uitk.Space10());

            dynamicSettingsContainer = new VisualElement();
            root.Add(dynamicSettingsContainer);
            UpdateDynamicSettings();

            root.Add(uitk.Space10());

            DrawTextureImporterSettings(root);

            return root;
        }

        private void DrawImageComponentPanel(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var settings = monoBeh.Settings.ImageSpritesSettings;
            var imageComponentField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_image_component), settings.ImageComponent);
            imageComponentField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_image_component);

            void ApplyImageComponentSelection(ImageComponent requestedValue, bool updateField, bool logErrors)
            {
                var validatedValue = requestedValue;
#if SUBC_SHAPES_EXISTS == false
                if (requestedValue == ImageComponent.SubcShape)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(ImageComponent.SubcShape)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

#if MPUIKIT_EXISTS == false
                if (requestedValue == ImageComponent.MPImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(ImageComponent.MPImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

#if JOSH_PUI_EXISTS == false
                if (requestedValue == ImageComponent.ProceduralImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(ImageComponent.ProceduralImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

#if PROCEDURAL_UI_ASSET_STORE_RELEASE == false
                if (requestedValue == ImageComponent.RoundedImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(ImageComponent.RoundedImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

#if VECTOR_GRAPHICS_EXISTS == false
                if (requestedValue == ImageComponent.SvgImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(ImageComponent.SvgImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif
#if FLEXIBLE_IMAGE_EXISTS == false
                if (requestedValue == ImageComponent.FlexibleImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(ImageComponent.FlexibleImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

                if (monoBeh.Settings.MainSettings.UIFramework == UIFramework.UITK &&
                    validatedValue != ImageComponent.UI_Toolkit_Image)
                {
                    Debug.LogError(scriptableObject.Localize(FcuLocKey.label_cannot_select_setting, validatedValue, monoBeh.Settings.MainSettings.UIFramework));
                    validatedValue = ImageComponent.UI_Toolkit_Image;
                }

                if (updateField)
                {
                    if ((ImageComponent)imageComponentField.value != validatedValue)
                    {
                        imageComponentField.SetValueWithoutNotify(validatedValue);
                    }
                }

                settings.ImageComponent = validatedValue;

                if (dynamicSettingsContainer != null)
                {
                    UpdateDynamicSettings();
                }
            }

            imageComponentField.RegisterValueChangedCallback(evt =>
            {
                ApplyImageComponentSelection((ImageComponent)evt.newValue, updateField: true, logErrors: true);
            });
            imageComponentField.AddResetMenu(settings, FcuDefaults.ImageSpritesSettings, s => s.ImageComponent, (s, v) => ApplyImageComponentSelection(v, updateField: true, logErrors: false));
            panel.Add(imageComponentField);

            ApplyImageComponentSelection(settings.ImageComponent, updateField: true, logErrors: false);
        }

        private void DrawGeneralSettings(VisualElement parent, SerializedProperty imageSpritesSettingsProp)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            if (monoBeh.IsUGUI())
            {
                if (monoBeh.UsingAnyProceduralImage() || monoBeh.IsDebug())
                {
                    var procConditionProp = imageSpritesSettingsProp.FindPropertyRelative(nameof(ImageSpritesSettings.ProceduralCondition));
                    if (procConditionProp != null && procConditionProp.hasVisibleChildren)
                    {
                        var procConditionField = new PropertyField(procConditionProp);
                        panel.Add(procConditionField);
                        panel.Add(uitk.ItemSeparator());
                    }
                }

                if (monoBeh.UsingSvgImage() || monoBeh.IsDebug())
                {
                    var svgConditionProp = imageSpritesSettingsProp.FindPropertyRelative(nameof(ImageSpritesSettings.SvgCondition));
                    if (svgConditionProp != null && svgConditionProp.hasVisibleChildren)
                    {
                        var svgConditionField = new PropertyField(svgConditionProp);
                        panel.Add(svgConditionField);
                        panel.Add(uitk.ItemSeparator());
                    }
                }

                if (monoBeh.UsingUnityImage() || monoBeh.UsingRawImage() || monoBeh.IsDebug())
                {
                    if (PlayerSettings.colorSpace == ColorSpace.Linear || monoBeh.IsDebug())
                    {
                        var linearMatToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_use_image_linear_material));
                        linearMatToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_use_image_linear_material);
                        linearMatToggle.value = monoBeh.Settings.ImageSpritesSettings.UseImageLinearMaterial;
                        linearMatToggle.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.UseImageLinearMaterial = evt.newValue);
                        panel.Add(linearMatToggle);
                        panel.Add(uitk.ItemSeparator());
                    }
                }
            }

            var imageFormatField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_images_format), monoBeh.Settings.ImageSpritesSettings.ImageFormat);
            imageFormatField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_images_format);

            void ApplyImageFormatSelection(ImageFormat requestedValue, bool updateField, bool logErrors)
            {
                var validatedValue = requestedValue;

#if VECTOR_GRAPHICS_EXISTS == false
                if (requestedValue == ImageFormat.SVG)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(ImageFormat.SVG)));
                    }

                    validatedValue = ImageFormat.PNG;
                }
#endif

                if (updateField)
                {
                    if ((ImageFormat)imageFormatField.value != validatedValue)
                    {
                        imageFormatField.SetValueWithoutNotify(validatedValue);
                    }
                }

                monoBeh.Settings.ImageSpritesSettings.ImageFormat = validatedValue;
            }

            imageFormatField.RegisterValueChangedCallback(evt =>
            {
                ApplyImageFormatSelection((ImageFormat)evt.newValue, updateField: true, logErrors: true);
            });
            imageFormatField.AddResetMenu(monoBeh.Settings.ImageSpritesSettings, FcuDefaults.ImageSpritesSettings, s => s.ImageFormat, (s, v) => ApplyImageFormatSelection(v, updateField: true, logErrors: false));
            panel.Add(imageFormatField);
            panel.Add(uitk.ItemSeparator());

            ApplyImageFormatSelection(monoBeh.Settings.ImageSpritesSettings.ImageFormat, updateField: true, logErrors: false);

            const float imageScaleStep = 0.25f;
            var imageScaleSlider = new Slider(scriptableObject.Localize(FcuLocKey.label_images_scale), 0.25f, 4.0f);
            imageScaleSlider.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_images_scale);
            imageScaleSlider.showInputField = true;

            float SnapImageScale(float value)
            {
                float snapped = Mathf.Round(value / imageScaleStep) * imageScaleStep;
                return Mathf.Clamp(snapped, imageScaleSlider.lowValue, imageScaleSlider.highValue);
            }

            float initialScale = SnapImageScale(monoBeh.Settings.ImageSpritesSettings.ImageScale);
            imageScaleSlider.SetValueWithoutNotify(initialScale);
            monoBeh.Settings.ImageSpritesSettings.ImageScale = initialScale;

            imageScaleSlider.RegisterValueChangedCallback(evt =>
            {
                float snappedValue = SnapImageScale(evt.newValue);
                monoBeh.Settings.ImageSpritesSettings.ImageScale = snappedValue;

                if (!Mathf.Approximately(snappedValue, evt.newValue))
                {
                    imageScaleSlider.SetValueWithoutNotify(snappedValue);
                }
            });
            imageScaleSlider.AddResetMenu(monoBeh.Settings.ImageSpritesSettings, FcuDefaults.ImageSpritesSettings, s => s.ImageScale, (s, v) => s.ImageScale = v);
            panel.Add(imageScaleSlider);
            panel.Add(uitk.ItemSeparator());

            List<int> maxSpriteSizeChoices = SpriteSizeConstants.MaxSpriteSizeValues.ToList();
            int currentMaxSpriteSize = monoBeh.Settings.ImageSpritesSettings.MaxSpriteSize;
            if (!maxSpriteSizeChoices.Contains(currentMaxSpriteSize))
            {
                currentMaxSpriteSize = SpriteSizeConstants.DefaultMaxSpriteSize;
                monoBeh.Settings.ImageSpritesSettings.MaxSpriteSize = currentMaxSpriteSize;
            }

            int currentMaxSpriteSizeIndex = maxSpriteSizeChoices.IndexOf(currentMaxSpriteSize);

            var maxSpriteSizeField = new PopupField<int>(
                scriptableObject.Localize(FcuLocKey.label_max_sprite_size),
                maxSpriteSizeChoices,
                currentMaxSpriteSizeIndex,
                v => v.ToString(),
                v => v.ToString());
            maxSpriteSizeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_max_sprite_size);
            if (maxSpriteSizeField.childCount > 1)
            {
                VisualElement input = maxSpriteSizeField[1];
                input.style.maxWidth = DAI_UitkConstants.FieldMaxWidthMedium;
                input.style.backgroundColor = uitk.ColorScheme.BUTTON;
                input.style.marginLeft = new StyleLength(StyleKeyword.Auto);
            }
            maxSpriteSizeField.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.MaxSpriteSize = evt.newValue);
            maxSpriteSizeField.AddResetMenu(monoBeh.Settings.ImageSpritesSettings, FcuDefaults.ImageSpritesSettings, s => s.MaxSpriteSize, (s, v) => s.MaxSpriteSize = v);
            panel.Add(maxSpriteSizeField);
            panel.Add(uitk.ItemSeparator());

            var ppuField = uitk.FloatField(scriptableObject.Localize(FcuLocKey.label_pixels_per_unit));
            ppuField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_pixels_per_unit);
            ppuField.value = monoBeh.Settings.ImageSpritesSettings.PixelsPerUnit;
            ppuField.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.PixelsPerUnit = evt.newValue);
            ppuField.AddResetMenu(monoBeh.Settings.ImageSpritesSettings, FcuDefaults.ImageSpritesSettings, s => s.PixelsPerUnit, (s, v) => s.PixelsPerUnit = v);
            panel.Add(ppuField);
            panel.Add(uitk.ItemSeparator());

            var redownloadToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_redownload_sprites));
            redownloadToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_redownload_sprites);
            redownloadToggle.value = monoBeh.Settings.ImageSpritesSettings.RedownloadSprites;
            redownloadToggle.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.RedownloadSprites = evt.newValue);
            redownloadToggle.AddResetMenu(monoBeh.Settings.ImageSpritesSettings, FcuDefaults.ImageSpritesSettings, s => s.RedownloadSprites, (s, v) => s.RedownloadSprites = v);
            panel.Add(redownloadToggle);
            panel.Add(uitk.ItemSeparator());

            var downloadOptionsField = uitk.EnumFlagsField("Download Options", monoBeh.Settings.ImageSpritesSettings.DownloadOptions);
            downloadOptionsField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.ImageSpritesSettings.DownloadOptions = (SpriteDownloadOptions)evt.newValue;
            });
            downloadOptionsField.AddResetMenu(monoBeh.Settings.ImageSpritesSettings, FcuDefaults.ImageSpritesSettings, s => s.DownloadOptions, (s, v) => s.DownloadOptions = v);
            panel.Add(downloadOptionsField);
            panel.Add(uitk.ItemSeparator());

            var preserveRatioField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_preserve_ratio_mode), monoBeh.Settings.ImageSpritesSettings.PreserveRatioMode);
            preserveRatioField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_preserve_ratio_mode);
            preserveRatioField.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.PreserveRatioMode = (PreserveRatioMode)evt.newValue);
            preserveRatioField.AddResetMenu(monoBeh.Settings.ImageSpritesSettings, FcuDefaults.ImageSpritesSettings, s => s.PreserveRatioMode, (s, v) => s.PreserveRatioMode = v);
            panel.Add(preserveRatioField);
            panel.Add(uitk.ItemSeparator());

            {
                var spritesPathContainer = uitk.CreateFolderInput(
                    label: scriptableObject.Localize(FcuLocKey.label_sprites_path),
                    tooltip: scriptableObject.Localize(FcuLocKey.tooltip_sprites_path),
                    initialValue: monoBeh.Settings.ImageSpritesSettings.SpritesPath,
                    onPathChanged: (newValue) => monoBeh.Settings.ImageSpritesSettings.SpritesPath = newValue,
                    onButtonClick: () => EditorUtility.OpenFolderPanel(
                        scriptableObject.Localize(FcuLocKey.label_select_folder),
                        monoBeh.Settings.ImageSpritesSettings.SpritesPath,
                        ""),
                    buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_folder));
                spritesPathContainer.AddFolderResetMenu(
                    () => monoBeh.Settings.ImageSpritesSettings.SpritesPath,
                    FcuDefaults.ImageSpritesSettings.SpritesPath,
                    v => monoBeh.Settings.ImageSpritesSettings.SpritesPath = v);
                panel.Add(spritesPathContainer);
            }
        }

        private void UpdateDynamicSettings()
        {
            dynamicSettingsContainer.Clear();

            if (monoBeh.IsUGUI() == false)
            {
                return;
            }

            switch (monoBeh.Settings.ImageSpritesSettings.ImageComponent)
            {
                case ImageComponent.UnityImage:
                    DrawUnityImageSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.RawImage:
                    DrawRawImageSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.ProceduralImage:
                    DrawProceduralUIImageSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.SubcShape:
                    DrawShapes2DSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.RoundedImage:
                    DrawDttPuiSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.MPImage:
                    DrawMPUIKitSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.FlexibleImage:
                    DrawFlexibleImageSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.SpriteRenderer:
                    DrawSpriteRendererSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.SvgImage:
                    DrawSvgImageSettings(dynamicSettingsContainer);
                    break;
            }
        }

        private void DrawBaseImageSettingsFields(VisualElement parent, BaseImageSettings settings)
        {
            var typeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_image_type), settings.Type);
            typeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_image_type);
            typeField.RegisterValueChangedCallback(evt => settings.Type = (UnityEngine.UI.Image.Type)evt.newValue);
            parent.Add(typeField);
            parent.Add(uitk.ItemSeparator());

            var raycastToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_raycast_target));
            raycastToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_raycast_target);
            raycastToggle.value = settings.RaycastTarget;
            raycastToggle.RegisterValueChangedCallback(evt => settings.RaycastTarget = evt.newValue);
            parent.Add(raycastToggle);
            parent.Add(uitk.ItemSeparator());

            var aspectToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_preserve_aspect));
            aspectToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_preserve_aspect);
            aspectToggle.value = settings.PreserveAspect;
            aspectToggle.RegisterValueChangedCallback(evt => settings.PreserveAspect = evt.newValue);
            parent.Add(aspectToggle);
            parent.Add(uitk.ItemSeparator());

            var paddingField = uitk.Vector4Field(scriptableObject.Localize(FcuLocKey.label_raycast_padding));
            paddingField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_raycast_padding);
            paddingField.value = settings.RaycastPadding;
            paddingField.RegisterValueChangedCallback(evt => settings.RaycastPadding = evt.newValue);
            parent.Add(paddingField);
            parent.Add(uitk.ItemSeparator());

            var maskableToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_maskable));
            maskableToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_maskable);
            maskableToggle.value = settings.Maskable;
            maskableToggle.RegisterValueChangedCallback(evt => settings.Maskable = evt.newValue);
            parent.Add(maskableToggle);
        }

        private void DrawUnityImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_unity_image_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_unity_image_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, monoBeh.Settings.UnityImageSettings);
        }

        private void DrawRawImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_raw_image_settings));
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, monoBeh.Settings.RawImageSettings);
        }

        private void DrawShapes2DSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_shapes2d_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_shapes2d_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, monoBeh.Settings.Shapes2DSettings);
        }

        private void DrawDttPuiSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_procedural_ui_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_procedural_ui_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.DttPuiSettings;

            var falloffField = uitk.FloatField(scriptableObject.Localize(FcuLocKey.label_pui_falloff_distance));
            falloffField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_pui_falloff_distance);
            falloffField.value = settings.FalloffDistance;
            falloffField.RegisterValueChangedCallback(evt => settings.FalloffDistance = evt.newValue);
            panel.Add(falloffField);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, settings);
        }

        private void DrawMPUIKitSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_mpuikit_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_mpuikit_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.MPUIKitSettings;

            var falloffField = uitk.FloatField(scriptableObject.Localize(FcuLocKey.label_pui_falloff_distance));
            falloffField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_pui_falloff_distance);
            falloffField.value = settings.FalloffDistance;
            falloffField.RegisterValueChangedCallback(evt => settings.FalloffDistance = evt.newValue);
            panel.Add(falloffField);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, settings);
        }

        private void DrawFlexibleImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_flexible_image_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_flexible_image_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.FlexibleImageSettings;

            var featherField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_feather_mode), settings.FeatherMode);
            featherField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_feather_mode);
            featherField.RegisterValueChangedCallback(evt =>
            {
                settings.FeatherMode = (FlexibleImageSettings.FlexibleImageFeatherMode)evt.newValue;
            });
            panel.Add(featherField);
            panel.Add(uitk.ItemSeparator());

            var softnessField = uitk.FloatField(scriptableObject.Localize(FcuLocKey.label_softness));
            softnessField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_softness);
            softnessField.value = settings.Softness;
            softnessField.RegisterValueChangedCallback(evt => settings.Softness = evt.newValue);
            panel.Add(softnessField);
            panel.Add(uitk.ItemSeparator());

            var meshSubdivisionsField = uitk.IntegerField(scriptableObject.Localize(FcuLocKey.label_mesh_subdivisions));
            meshSubdivisionsField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_mesh_subdivisions);
            meshSubdivisionsField.value = settings.MeshSubdivisions;
            meshSubdivisionsField.RegisterValueChangedCallback(evt => settings.MeshSubdivisions = evt.newValue);
            panel.Add(meshSubdivisionsField);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, settings);
        }

        private void DrawSpriteRendererSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_sr_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_sr_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.SpriteRendererSettings;

            var flipXToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_flip_x));
            flipXToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_flip_x);
            flipXToggle.value = settings.FlipX;
            flipXToggle.RegisterValueChangedCallback(evt => settings.FlipX = evt.newValue);
            panel.Add(flipXToggle);
            panel.Add(uitk.ItemSeparator());

            var flipYToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_flip_y));
            flipYToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_flip_y);
            flipYToggle.value = settings.FlipY;
            flipYToggle.RegisterValueChangedCallback(evt => settings.FlipY = evt.newValue);
            panel.Add(flipYToggle);
            panel.Add(uitk.ItemSeparator());

            var maskInteractionField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_mask_interaction), settings.MaskInteraction);
            maskInteractionField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_mask_interaction);
            maskInteractionField.RegisterValueChangedCallback(evt => settings.MaskInteraction = (SpriteMaskInteraction)evt.newValue);
            panel.Add(maskInteractionField);
            panel.Add(uitk.ItemSeparator());

            var sortPointField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_sort_point), settings.SortPoint);
            sortPointField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_sort_point);
            sortPointField.RegisterValueChangedCallback(evt => settings.SortPoint = (SpriteSortPoint)evt.newValue);
            panel.Add(sortPointField);
            panel.Add(uitk.ItemSeparator());

            var sortingLayerField = uitk.TextField(scriptableObject.Localize(FcuLocKey.label_sorting_layer));
            sortingLayerField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_sorting_layer);
            sortingLayerField.value = settings.SortingLayer;
            sortingLayerField.RegisterValueChangedCallback(evt => settings.SortingLayer = evt.newValue);
            panel.Add(sortingLayerField);
            panel.Add(uitk.ItemSeparator());

            var nextOrderStepField = uitk.IntegerField(scriptableObject.Localize(FcuLocKey.label_next_order_step));
            nextOrderStepField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_next_order_step);
            nextOrderStepField.value = settings.NextOrderStep;
            nextOrderStepField.RegisterValueChangedCallback(evt => settings.NextOrderStep = evt.newValue);
            panel.Add(nextOrderStepField);
        }

        private void DrawProceduralUIImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_pui_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_pui_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.JoshPuiSettings;

            var falloffField = uitk.FloatField(scriptableObject.Localize(FcuLocKey.label_pui_falloff_distance));
            falloffField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_pui_falloff_distance);
            falloffField.value = settings.FalloffDistance;
            falloffField.RegisterValueChangedCallback(evt => settings.FalloffDistance = evt.newValue);
            panel.Add(falloffField);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, settings);
        }

        private void DrawTextureImporterSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_texture_importer_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_texture_importer_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.TextureImporterSettings;

            var crunchToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_crunched_compression));
            crunchToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_crunched_compression);
            crunchToggle.value = settings.CrunchedCompression;

            var qualitySlider = uitk.SliderInt(scriptableObject.Localize(FcuLocKey.label_compression_quality), 0, 100);
            qualitySlider.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_compression_quality);
            qualitySlider.showInputField = true;
            qualitySlider.value = settings.CompressionQuality;
            qualitySlider.RegisterValueChangedCallback(evt => settings.CompressionQuality = evt.newValue);
            qualitySlider.SetEnabled(settings.CrunchedCompression);

            crunchToggle.RegisterValueChangedCallback(evt =>
            {
                settings.CrunchedCompression = evt.newValue;
                qualitySlider.SetEnabled(evt.newValue);
            });

            panel.Add(crunchToggle);
            panel.Add(uitk.ItemSeparator());
            panel.Add(qualitySlider);
            panel.Add(uitk.ItemSeparator());

            var readableToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_is_readable));
            readableToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_is_readable);
            readableToggle.value = settings.IsReadable;
            readableToggle.RegisterValueChangedCallback(evt => settings.IsReadable = evt.newValue);
            panel.Add(readableToggle);
            panel.Add(uitk.ItemSeparator());

            var mipmapToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_mipmap_enabled));
            mipmapToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_mipmap_enabled);
            mipmapToggle.value = settings.MipmapEnabled;
            mipmapToggle.RegisterValueChangedCallback(evt => settings.MipmapEnabled = evt.newValue);
            panel.Add(mipmapToggle);
            panel.Add(uitk.ItemSeparator());

            var typeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_texture_type), settings.TextureType);
            typeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_texture_type);
            typeField.RegisterValueChangedCallback(evt => settings.TextureType = (TextureImporterType)evt.newValue);
            panel.Add(typeField);
            panel.Add(uitk.ItemSeparator());

            var compressionField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_texture_compression), settings.TextureCompression);
            compressionField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_texture_compression);
            compressionField.RegisterValueChangedCallback(evt => settings.TextureCompression = (TextureImporterCompression)evt.newValue);
            panel.Add(compressionField);
            panel.Add(uitk.ItemSeparator());

            var spriteModeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_sprite_import_mode), settings.SpriteImportMode);
            spriteModeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_sprite_import_mode);
            spriteModeField.RegisterValueChangedCallback(evt => settings.SpriteImportMode = (SpriteImportMode)evt.newValue);
            panel.Add(spriteModeField);
        }

        private void DrawSvgImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(scriptableObject.Localize(FcuLocKey.label_svg_image_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_svg_image_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.SvgImageSettings;

            var raycastToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_raycast_target));
            raycastToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_raycast_target);
            raycastToggle.value = settings.RaycastTarget;
            raycastToggle.RegisterValueChangedCallback(evt => settings.RaycastTarget = evt.newValue);
            panel.Add(raycastToggle);
            panel.Add(uitk.ItemSeparator());

            var aspectToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_preserve_aspect));
            aspectToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_preserve_aspect);
            aspectToggle.value = settings.PreserveAspect;
            aspectToggle.RegisterValueChangedCallback(evt => settings.PreserveAspect = evt.newValue);
            panel.Add(aspectToggle);
            panel.Add(uitk.ItemSeparator());

            var paddingField = uitk.Vector4Field(scriptableObject.Localize(FcuLocKey.label_raycast_padding));
            paddingField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_raycast_padding);
            paddingField.value = settings.RaycastPadding;
            paddingField.RegisterValueChangedCallback(evt => settings.RaycastPadding = evt.newValue);
            panel.Add(paddingField);
        }
    }
}