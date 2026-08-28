using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace DA_Assets.Figmage
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/Figmage")]
    public sealed class Figmage : BaseMeshEffect
    {
        const float Epsilon = 0.0001f;

        [SerializeField, Range(0f, 1f)] float opacity = 1f;
        [SerializeField] float cornerRadius;
        [SerializeField] List<float> cornerRadiuses = new List<float>();
        [SerializeField, Range(0f, 1f)] float cornerSmoothing;
        [SerializeField] List<FigmagePaint> fills = new List<FigmagePaint>
        {
            FigmagePaint.Solid(Color.white)
        };
        [SerializeField] List<FigmagePaint> strokes = new List<FigmagePaint>();
        [SerializeField] float strokeWeight;
        [SerializeField] FigmageStrokeAlign strokeAlign = FigmageStrokeAlign.Center;
        [SerializeField] FigmageStrokeJoin strokeJoin = FigmageStrokeJoin.Miter;
        [SerializeField] FigmageStrokeCap strokeCap = FigmageStrokeCap.None;
        [SerializeField] float strokeMiterLimit = 4f;
        [SerializeField] List<float> dashPattern = new List<float>();
        [SerializeField] FigmageStrokeWeights individualStrokeWeights;
        [SerializeField] List<FigmageEffect> effects = new List<FigmageEffect>();
        [SerializeField] List<FigmageGeometry> fillGeometry;
        [SerializeField] List<FigmageGeometry> strokeGeometry;
        [SerializeField] List<FigmageNode> children;
        [SerializeField] FigmageNode sourceNode;
        [SerializeField] bool hasSourceBounds;
        [SerializeField] FigmageRect sourceContentBounds;
        [SerializeField] FigmageRect sourceRenderBounds;
        [SerializeField] float sourceRotation;
        [SerializeField] bool hasSourceRelativeTransform;
        [SerializeField] Vector3 sourceRelativeTransformRow0;
        [SerializeField] Vector3 sourceRelativeTransformRow1;
        [SerializeField] bool shaderEnabled = true;
        [SerializeField] string bakedSpriteGuid;
        [SerializeField] FigmageOverflowAnchors overflowAnchors;

        [NonSerialized] Image image;
        [NonSerialized] RectTransform rectTransform;
        [NonSerialized] Sprite livePreviewSprite;
        [NonSerialized] Texture2D livePreviewTexture;
        [NonSerialized] bool refreshQueued;
        [NonSerialized] bool hasRenderBoundsOverride;
        [NonSerialized] Rect renderBoundsOverride;

        public List<FigmagePaint> Fills => fills;
        public List<FigmagePaint> Strokes => strokes;
        public List<FigmageEffect> Effects => effects;
        public List<float> DashPattern => dashPattern;
        public List<FigmageGeometry> FillGeometry => fillGeometry;
        public List<FigmageGeometry> StrokeGeometry => strokeGeometry;
        public List<FigmageNode> Children => children;
        public List<float> CornerRadiuses => cornerRadiuses;
        public FigmageStrokeWeights IndividualStrokeWeights
        {
            get => individualStrokeWeights;
            set => individualStrokeWeights = ClampStrokeWeights(value);
        }
        public FigmageOverflowAnchors OverflowAnchors => overflowAnchors;
        public string BakedSpriteGuid
        {
            get => bakedSpriteGuid;
            set => bakedSpriteGuid = value;
        }

        public bool ShaderEnabled
        {
            get => shaderEnabled;
            set
            {
                if (shaderEnabled == value)
                    return;

                shaderEnabled = value;
                if (!shaderEnabled)
                    ReleaseLivePreview();
                Refresh();
            }
        }

        public Image Image
        {
            get
            {
                if (image == null)
                    image = GetComponent<Image>();
                return image;
            }
        }

        RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = transform as RectTransform;
                return rectTransform;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                RefreshImmediate();
                return;
            }
#endif
            Refresh();
        }

        protected override void OnDisable()
        {
            ReleaseLivePreview();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Refresh();
        }

        void OnValidate()
        {
            opacity = Mathf.Clamp01(opacity);
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSmoothing = Mathf.Clamp01(cornerSmoothing);
            ClampCornerRadiuses(cornerRadiuses);
            strokeWeight = Mathf.Max(0f, strokeWeight);
            strokeMiterLimit = Mathf.Max(1f, strokeMiterLimit);
            individualStrokeWeights = ClampStrokeWeights(individualStrokeWeights);
            ClampFloatList(dashPattern);
            Refresh();
        }

#if UNITY_EDITOR
        void Update()
        {
            if (!Application.isPlaying && shaderEnabled && (livePreviewSprite == null || Image.sprite != livePreviewSprite))
                RefreshImmediate();
        }
#endif

        public void Refresh()
        {
            if (!isActiveAndEnabled || !shaderEnabled)
            {
                SetVerticesDirty();
                return;
            }

            QueueRefresh();
        }

        public void RefreshImmediate()
        {
            refreshQueued = false;
            RefreshPreviewNow();
        }

        public void ApplyNode(FigmageNode node, Rect? renderBoundsOverride = null)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            name = string.IsNullOrWhiteSpace(node.Name) ? node.Id : node.Name;

            Vector2 size = ResolveNodeSize(node);
            RectTransform.sizeDelta = size;

            opacity = Mathf.Clamp01(node.Opacity);
            cornerRadius = Mathf.Max(0f, node.CornerRadius);
            cornerRadiuses = CloneFloatList(node.CornerRadiuses) ?? new List<float>();
            cornerSmoothing = Mathf.Clamp01(node.CornerSmoothing);
            strokeWeight = Mathf.Max(0f, node.StrokeWeight);
            strokeAlign = node.StrokeAlign;
            strokeJoin = node.StrokeJoin;
            strokeCap = node.StrokeCap;
            strokeMiterLimit = Mathf.Max(1f, node.StrokeMiterLimit);
            dashPattern = CloneFloatList(node.DashPattern) ?? new List<float>();
            individualStrokeWeights = ClampStrokeWeights(node.IndividualStrokeWeights);
            fills = ClonePaintList(node.Fills);
            strokes = ClonePaintList(node.Strokes);
            effects = CloneEffectList(node.Effects);
            fillGeometry = CloneGeometryList(node.FillGeometry);
            strokeGeometry = CloneGeometryList(node.StrokeGeometry);
            children = CloneNodeList(node.Children);
            sourceNode = CloneNode(node);

            Rect contentBounds = ResolveNodeContentBounds(node);
            Rect renderBounds = renderBoundsOverride ?? FigmageBoundsUtility.CalculateRenderBounds(node);
            sourceContentBounds = ToFigmageRect(contentBounds);
            sourceRenderBounds = ToFigmageRect(renderBounds);
            sourceRotation = node.Rotation;
            SetSourceRelativeTransform(node.RelativeTransform);
            hasSourceBounds = true;
            this.renderBoundsOverride = ToLocalRenderBounds(contentBounds, renderBounds);
            hasRenderBoundsOverride = true;
            ApplyOverflowFromBounds(new Rect(Vector2.zero, size), this.renderBoundsOverride);
            RefreshImmediate();
        }

        public byte[] BakePngBytes(float scale = 1f)
        {
            FigmageNode node = ToNode();
            return FigmageBakeUtility.BakePngBytes(node, scale, ResolveBakeRenderBounds(node));
        }

        public void BakePng(string outputPath, float scale = 1f)
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            FigmageNode node = ToNode();
            FigmageBakeUtility.BakePng(node, outputPath, scale, ResolveBakeRenderBounds(node));
        }

        public FigmageNode ToNode()
        {
            Vector2 resolvedSize = ResolveSize();
            Rect contentBounds = hasSourceBounds
                ? ToRect(sourceContentBounds)
                : new Rect(0f, 0f, resolvedSize.x, resolvedSize.y);

            FigmageNode node = new FigmageNode();

            if (sourceNode != null)
            {
                node.Id = sourceNode.Id;
                node.Name = sourceNode.Name;
            }
            else
            {
                node.Id = name;
                node.Name = name;
            }
            node.AbsoluteBoundingBox = ToFigmageRect(contentBounds);
            node.AbsoluteRenderBounds = hasSourceBounds ? sourceRenderBounds : ToFigmageRect(contentBounds);
            node.Size = hasSourceBounds && sourceNode != null ? sourceNode.Size : resolvedSize;
            node.Rotation = hasSourceBounds ? sourceRotation : 0f;
            node.RelativeTransform = hasSourceRelativeTransform ? BuildSourceRelativeTransform() : null;
            node.Opacity = opacity;
            node.CornerRadius = cornerRadius;
            node.CornerRadiuses = CloneFloatList(cornerRadiuses);
            node.CornerSmoothing = cornerSmoothing;
            node.StrokeWeight = strokeWeight;
            node.StrokeAlign = strokeAlign;
            node.StrokeJoin = strokeJoin;
            node.StrokeCap = strokeCap;
            node.StrokeMiterLimit = strokeMiterLimit;
            node.DashPattern = CloneFloatList(dashPattern);
            node.IndividualStrokeWeights = individualStrokeWeights;
            node.Fills = ClonePaintList(fills);
            node.Strokes = ClonePaintList(strokes);
            node.Effects = CloneEffectList(effects);
            bool cornerStyleChanged = HasSourceCornerStyleChanged();
            node.FillGeometry = cornerStyleChanged ? null : CloneGeometryList(fillGeometry);
            node.StrokeGeometry = cornerStyleChanged ? null : CloneGeometryList(strokeGeometry);
            node.Children = CloneNodeList(children);
            return node;
        }

        bool HasSourceCornerStyleChanged()
        {
            if (sourceNode == null)
                return false;

            return !Approximately(cornerRadius, sourceNode.CornerRadius) ||
                   !Approximately(cornerSmoothing, sourceNode.CornerSmoothing) ||
                   !FloatListsMatch(cornerRadiuses, sourceNode.CornerRadiuses);
        }

        static bool FloatListsMatch(List<float> a, List<float> b)
        {
            int aCount = a?.Count ?? 0;
            int bCount = b?.Count ?? 0;
            if (aCount != bCount)
                return false;

            for (int i = 0; i < aCount; i++)
            {
                if (!Approximately(a[i], b[i]))
                    return false;
            }

            return true;
        }

        static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= Epsilon;
        }

        public void ApplyOverflowFromBounds(Rect contentBounds, Rect renderBounds)
        {
            SetOverflowFromBounds(contentBounds, renderBounds, true);
        }

        public void ApplyCurrentOverflow()
        {
            FigmageNode node = ToNode();
            Vector2 size = ResolveSize();
            SetOverflowFromBounds(new Rect(Vector2.zero, size), ResolveLocalRenderBounds(node), true);
        }

        void SetOverflowFromBounds(Rect contentBounds, Rect renderBounds, bool setVerticesDirty)
        {
            Vector2 size = contentBounds.size;
            float width = Mathf.Max(Epsilon, size.x);
            float height = Mathf.Max(Epsilon, size.y);

            overflowAnchors = new FigmageOverflowAnchors(
                Mathf.Max(0f, contentBounds.xMin - renderBounds.xMin) / width,
                Mathf.Max(0f, renderBounds.xMax - contentBounds.xMax) / width,
                Mathf.Max(0f, contentBounds.yMin - renderBounds.yMin) / height,
                Mathf.Max(0f, renderBounds.yMax - contentBounds.yMax) / height);

            if (setVerticesDirty)
                SetVerticesDirty();
        }

        public void ClearOverflow()
        {
            hasSourceBounds = false;
            hasRenderBoundsOverride = false;
            overflowAnchors = default;
            SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || !HasOverflow() || Image.sprite == null)
                return;

            Rect rect = Image.GetPixelAdjustedRect();
            float width = rect.width;
            float height = rect.height;

            rect.xMin -= overflowAnchors.Left * width;
            rect.xMax += overflowAnchors.Right * width;
            rect.yMin -= overflowAnchors.Bottom * height;
            rect.yMax += overflowAnchors.Top * height;

            Vector4 uv = Image.overrideSprite != null
                ? DataUtility.GetInnerUV(Image.overrideSprite)
                : new Vector4(0f, 0f, 1f, 1f);

            Color color = Image.color;
            vh.Clear();
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, new Vector2(uv.x, uv.y));
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, new Vector2(uv.x, uv.w));
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, new Vector2(uv.z, uv.w));
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, new Vector2(uv.z, uv.y));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        void QueueRefresh()
        {
            if (refreshQueued)
                return;

            refreshQueued = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += RefreshPreviewNow;
                return;
            }
#endif
            RefreshPreviewNow();
        }

        void RefreshPreviewNow()
        {
            refreshQueued = false;

            if (!this || !isActiveAndEnabled || !shaderEnabled)
                return;

            Vector2 size = ResolveSize();
            if (size.x <= Epsilon || size.y <= Epsilon)
                return;

            FigmageNode node = ToNode();
            Rect contentBounds = new Rect(0f, 0f, size.x, size.y);
            Rect bakeRenderBounds = ResolveBakeRenderBounds(node);
            byte[] png = FigmageBakeUtility.BakePngBytes(node, 1f, bakeRenderBounds);

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                name = $"{name} Figmage Preview",
                hideFlags = HideFlags.HideAndDontSave
            };

            if (!texture.LoadImage(png))
            {
                DestroyUnityObject(texture);
                return;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = $"{name} Figmage Preview";
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Sprite previousSprite = livePreviewSprite;
            Texture2D previousTexture = livePreviewTexture;
            livePreviewSprite = sprite;
            livePreviewTexture = texture;

            SetOverflowFromBounds(contentBounds, ResolveLocalRenderBounds(node), false);
            Image.sprite = sprite;
            Image.material = null;
            Image.color = Color.white;
            SetVerticesDirty();

            DestroyUnityObject(previousSprite);
            DestroyUnityObject(previousTexture);
        }

        Vector2 ResolveSize()
        {
            Rect rect = RectTransform != null ? RectTransform.rect : default;
            return new Vector2(Mathf.Max(1f, rect.width), Mathf.Max(1f, rect.height));
        }

        bool HasOverflow()
        {
            return overflowAnchors.Left > Epsilon ||
                   overflowAnchors.Right > Epsilon ||
                   overflowAnchors.Top > Epsilon ||
                   overflowAnchors.Bottom > Epsilon;
        }

        Rect ResolveLocalRenderBounds()
        {
            return ResolveLocalRenderBounds(ToNode());
        }

        Rect ResolveLocalRenderBounds(FigmageNode node)
        {
            if (hasRenderBoundsOverride)
                return renderBoundsOverride;

            if (hasSourceBounds)
                return ToLocalRenderBounds(ToRect(sourceContentBounds), ToRect(sourceRenderBounds));

            return FigmageBoundsUtility.CalculateRenderBounds(node);
        }

        Rect ResolveBakeRenderBounds(FigmageNode node)
        {
            if (hasSourceBounds)
                return ToRect(sourceRenderBounds);

            return ResolveLocalRenderBounds(node);
        }

        void SetVerticesDirty()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        void ReleaseLivePreview()
        {
            DestroyUnityObject(livePreviewSprite);
            DestroyUnityObject(livePreviewTexture);
            livePreviewSprite = null;
            livePreviewTexture = null;
        }

        static List<FigmagePaint> ClonePaintList(List<FigmagePaint> source)
        {
            List<FigmagePaint> result = new List<FigmagePaint>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
                result.Add(FigmageStyleStack.ClonePaint(source[i]));

            return result;
        }

        static List<FigmageEffect> CloneEffectList(List<FigmageEffect> source)
        {
            List<FigmageEffect> result = new List<FigmageEffect>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
                result.Add(FigmageStyleStack.CloneEffect(source[i]));

            return result;
        }

        static List<float> CloneFloatList(List<float> source)
        {
            if (source == null || source.Count == 0)
                return null;

            List<float> result = new List<float>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(Mathf.Max(0f, source[i]));

            return result;
        }

        static List<FigmageGeometry> CloneGeometryList(List<FigmageGeometry> source)
        {
            if (source == null || source.Count == 0)
                return null;

            List<FigmageGeometry> result = new List<FigmageGeometry>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(source[i]);

            return result;
        }

        static List<FigmageNode> CloneNodeList(List<FigmageNode> source)
        {
            if (source == null || source.Count == 0)
                return null;

            List<FigmageNode> result = new List<FigmageNode>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(CloneNode(source[i]));

            return result;
        }

        static FigmageNode CloneNode(FigmageNode source)
        {
            if (source == null)
                return null;

            return new FigmageNode
            {
                Id = source.Id,
                Name = source.Name,
                AbsoluteBoundingBox = source.AbsoluteBoundingBox,
                AbsoluteRenderBounds = source.AbsoluteRenderBounds,
                Size = source.Size,
                Rotation = source.Rotation,
                RelativeTransform = CloneTransform(source.RelativeTransform),
                Opacity = source.Opacity,
                CornerRadius = source.CornerRadius,
                CornerRadiuses = CloneFloatList(source.CornerRadiuses),
                CornerSmoothing = source.CornerSmoothing,
                StrokeWeight = source.StrokeWeight,
                StrokeAlign = source.StrokeAlign,
                StrokeJoin = source.StrokeJoin,
                StrokeCap = source.StrokeCap,
                StrokeMiterLimit = source.StrokeMiterLimit,
                DashPattern = CloneFloatList(source.DashPattern),
                IndividualStrokeWeights = source.IndividualStrokeWeights,
                Fills = ClonePaintList(source.Fills),
                Strokes = ClonePaintList(source.Strokes),
                Effects = CloneEffectList(source.Effects),
                FillGeometry = CloneGeometryList(source.FillGeometry),
                StrokeGeometry = CloneGeometryList(source.StrokeGeometry),
                Children = CloneNodeList(source.Children)
            };
        }

        static List<List<float>> CloneTransform(List<List<float>> source)
        {
            if (source == null || source.Count == 0)
                return null;

            List<List<float>> result = new List<List<float>>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                List<float> row = source[i];
                result.Add(row != null ? new List<float>(row) : null);
            }

            return result;
        }

        void SetSourceRelativeTransform(List<List<float>> source)
        {
            hasSourceRelativeTransform = source != null &&
                                         source.Count >= 2 &&
                                         source[0] != null &&
                                         source[0].Count >= 3 &&
                                         source[1] != null &&
                                         source[1].Count >= 3;

            if (!hasSourceRelativeTransform)
            {
                sourceRelativeTransformRow0 = default;
                sourceRelativeTransformRow1 = default;
                return;
            }

            sourceRelativeTransformRow0 = new Vector3(source[0][0], source[0][1], source[0][2]);
            sourceRelativeTransformRow1 = new Vector3(source[1][0], source[1][1], source[1][2]);
        }

        List<List<float>> BuildSourceRelativeTransform()
        {
            return new List<List<float>>
            {
                new List<float> { sourceRelativeTransformRow0.x, sourceRelativeTransformRow0.y, sourceRelativeTransformRow0.z },
                new List<float> { sourceRelativeTransformRow1.x, sourceRelativeTransformRow1.y, sourceRelativeTransformRow1.z }
            };
        }

        static Vector2 ResolveNodeSize(FigmageNode node)
        {
            if (node.AbsoluteBoundingBox.Width > 0f && node.AbsoluteBoundingBox.Height > 0f)
                return new Vector2(node.AbsoluteBoundingBox.Width, node.AbsoluteBoundingBox.Height);

            return new Vector2(Mathf.Max(1f, node.Size.x), Mathf.Max(1f, node.Size.y));
        }

        static Rect ResolveNodeContentBounds(FigmageNode node)
        {
            Vector2 size = ResolveNodeSize(node);
            if (node.AbsoluteBoundingBox.Width > 0f && node.AbsoluteBoundingBox.Height > 0f)
            {
                return new Rect(
                    node.AbsoluteBoundingBox.X,
                    node.AbsoluteBoundingBox.Y,
                    node.AbsoluteBoundingBox.Width,
                    node.AbsoluteBoundingBox.Height);
            }

            return new Rect(0f, 0f, size.x, size.y);
        }

        static Rect ToLocalRenderBounds(Rect contentBounds, Rect renderBounds)
        {
            return Rect.MinMaxRect(
                renderBounds.xMin - contentBounds.xMin,
                renderBounds.yMin - contentBounds.yMin,
                renderBounds.xMax - contentBounds.xMin,
                renderBounds.yMax - contentBounds.yMin);
        }

        static Rect ToRect(FigmageRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        static FigmageRect ToFigmageRect(Rect rect)
        {
            return new FigmageRect(rect.x, rect.y, rect.width, rect.height);
        }

        static void ClampCornerRadiuses(List<float> radiuses)
        {
            if (radiuses == null)
                return;

            for (int i = 0; i < radiuses.Count; i++)
                radiuses[i] = Mathf.Max(0f, radiuses[i]);
        }

        static void ClampFloatList(List<float> values)
        {
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
                values[i] = Mathf.Max(0f, values[i]);
        }

        static FigmageStrokeWeights ClampStrokeWeights(FigmageStrokeWeights weights)
        {
            return new FigmageStrokeWeights(weights.Top, weights.Bottom, weights.Left, weights.Right);
        }

        static void DestroyUnityObject(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
                return;

            if (Application.isPlaying)
                Destroy(unityObject);
            else
                DestroyImmediate(unityObject);
        }
    }

    [Serializable]
    public struct FigmageOverflowAnchors
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;

        public FigmageOverflowAnchors(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }
}
