using System.Collections.Generic;
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.Singleton;
using DA_Assets.UpdateChecker.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static DA_Assets.DAI.DAInspectorUITK;

namespace DA_Assets.UCC
{
    public partial class ConverterEditorBase : UnityEditor.Editor, IDAEditor
    {
        protected internal virtual void PopulatePlatformTabs(FcuSettingsWindow window, List<FcuSettingsWindow.ITab> tabs, int insertIndex) { }
        protected internal virtual void PopulatePlatformMainSettings(FcuSettingsWindow window, VisualElement container) { }
        public ConverterBase monoBeh;

        [SerializeField] DAInspector _gui;
        public DAInspector gui { get => _gui; set => _gui = value; }

        [SerializeField] DAInspectorUITK _uitk;
        public DAInspectorUITK uitk { get => _uitk; set => _uitk = value; }

        protected internal virtual AssetType assetType => AssetType.FCU;

        protected internal string Localize(FcuLocKey key, params object[] args) =>
            monoBeh.Config.Localizator.Init().GetLocalizedText(key, null, args);

        [SerializeField] Texture2D _logoDarkTheme;
        public Texture2D LogoDarkTheme => _logoDarkTheme;

        [SerializeField] Texture2D _logoLightTheme;
        public Texture2D LogoLightTheme => _logoLightTheme;

        [SerializeField] Texture2D _topBanner;

        [SerializeField] Texture2D _gradientBg;

        [SerializeField] Texture2D _noise;

        private HeaderSection _headerSection;
        internal HeaderSection Header => monoBeh.Link(ref _headerSection, this);

        private FramesSection _frameListSection;
        internal FramesSection FrameList => monoBeh.Link(ref _frameListSection, this);

        private ScrollView _framesScroll;
        private VisualElement _framesHost;
        private SquareIconButton _btnRecent;
        private VisualElement _root;
        private TextField _projectUrlField;

        public override VisualElement CreateInspectorGUI()
        {
            if (monoBeh.Settings.MainSettings.WindowMode)
            {
                return DrawWindowedGUI();
            }

            return DrawGUI();
        }

        public VisualElement DrawGUI()
        {
            _root = uitk.CreateRoot(uitk.ColorScheme.BG);
            BuildContent();
            return _root;
        }

        internal void RebuildUI()
        {
            _root.Clear();
            BuildContent();
        }

        void ForceRebuild() => ActiveEditorTracker.sharedTracker.ForceRebuild();

        private VisualElement TopBannerOverlayCompat(Texture2D tex)
        {
            var wrap = new VisualElement()
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    left = new StyleLength(0f),
                    right = new StyleLength(0f),
                    top = new StyleLength(0f),
                    width = new StyleLength(Length.Percent(100)),
                    height = new StyleLength(StyleKeyword.Auto)
                }
            };

            if (tex == null)
            {
                wrap.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                return wrap;
            }

#if UNITY_6000_1_OR_NEWER
            var img = new Image()
            {
                image = tex,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width = new StyleLength(Length.Percent(100)),
                    height = new StyleLength(StyleKeyword.Auto)
                }
            };

            img.style.aspectRatio = new StyleRatio((float)tex.width / tex.height);
            wrap.Add(img);
#else
            float aspect = (float)tex.height / tex.width;
            var img = new IMGUIContainer(() =>
            {
                float width = wrap.resolvedStyle.width;
                float height = wrap.resolvedStyle.height;

                if (Event.current.type == EventType.Repaint && width > 0f && height > 0f)
                {
                    GUI.DrawTexture(new Rect(0f, 0f, width, height), tex, ScaleMode.StretchToFill, true);
                }
            })
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    left = new StyleLength(0f),
                    right = new StyleLength(0f),
                    top = new StyleLength(0f),
                    bottom = new StyleLength(0f)
                }
            };

            void Resize()
            {
                float width = wrap.parent?.resolvedStyle.width ?? 0f;
                if (float.IsNaN(width) || width <= 0f)
                {
                    width = wrap.resolvedStyle.width;
                }

                if (float.IsNaN(width) || width <= 0f)
                {
                    return;
                }

                wrap.style.width = new StyleLength(width);
                wrap.style.height = new StyleLength(width * aspect);
                img.MarkDirtyRepaint();
            }

            wrap.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                wrap.schedule.Execute(Resize);
                wrap.parent?.RegisterCallback<GeometryChangedEvent>(_ => Resize());
            });
            wrap.RegisterCallback<GeometryChangedEvent>(_ => Resize());
            wrap.Add(img);
#endif

            return wrap;
        }

        private VisualElement GradientBackgroundCompat(Texture2D tex)
        {
            var wrap = new VisualElement()
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    left = new StyleLength(0f),
                    right = new StyleLength(0f),
                    top = new StyleLength(0f),
                    bottom = new StyleLength(0f)
                }
            };

            if (tex == null)
            {
                wrap.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                return wrap;
            }

            var img = new IMGUIContainer(() =>
            {
                float width = wrap.resolvedStyle.width;
                float height = wrap.resolvedStyle.height;

                if (Event.current.type == EventType.Repaint && width > 0f && height > 0f)
                {
                    GUI.DrawTexture(new Rect(0f, 0f, width, height), tex, ScaleMode.StretchToFill, true);
                }
            })
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    left = new StyleLength(0f),
                    right = new StyleLength(0f),
                    top = new StyleLength(0f),
                    bottom = new StyleLength(0f)
                }
            };

            wrap.RegisterCallback<GeometryChangedEvent>(_ => img.MarkDirtyRepaint());
            wrap.Add(img);
            return wrap;
        }

        private VisualElement NoiseOverlayCompat(Texture2D tex)
        {
            var wrap = new VisualElement()
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    left = new StyleLength(0f),
                    right = new StyleLength(0f),
                    top = new StyleLength(0f),
                    bottom = new StyleLength(0f)
                }
            };

            if (tex == null)
            {
                wrap.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                return wrap;
            }

            var img = new IMGUIContainer(() =>
            {
                float width = wrap.resolvedStyle.width;
                float height = wrap.resolvedStyle.height;

                if (Event.current.type != EventType.Repaint || width <= 0f || height <= 0f)
                {
                    return;
                }

                float tileW = Mathf.Max(1f, tex.width / EditorGUIUtility.pixelsPerPoint);
                float tileH = Mathf.Max(1f, tex.height / EditorGUIUtility.pixelsPerPoint);

                for (float y = 0f; y < height; y += tileH)
                {
                    for (float x = 0f; x < width; x += tileW)
                    {
                        GUI.DrawTexture(new Rect(x, y, tileW, tileH), tex, ScaleMode.StretchToFill, true);
                    }
                }
            })
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    left = new StyleLength(0f),
                    right = new StyleLength(0f),
                    top = new StyleLength(0f),
                    bottom = new StyleLength(0f)
                }
            };

            wrap.RegisterCallback<GeometryChangedEvent>(_ => img.MarkDirtyRepaint());
            wrap.Add(img);
            return wrap;
        }

        internal void AddDecorativeBackground(VisualElement root, bool includeTopBanner)
        {
            if (monoBeh.Config.ShowDecorativeEditorBackground == false)
            {
                return;
            }

            if (includeTopBanner)
            {
                root.Add(TopBannerOverlayCompat(_topBanner));
            }

            root.Add(NoiseOverlayCompat(_noise));
        }
    }
}