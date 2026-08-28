using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DA_Assets.Figmage
{
    public sealed partial class FigmageRenderer : IDisposable
    {
        private const string ShadowSigmaScaleEnv = "DA_SHADOW_SIGMA_SCALE";
        private const string ShadowSpreadScaleEnv = "DA_SHADOW_SPREAD_SCALE";
        private const string ShadowAlphaScaleEnv = "DA_SHADOW_ALPHA_SCALE";
        private const string ShadowOffsetXScaleEnv = "DA_SHADOW_OFFSET_X_SCALE";
        private const string ShadowOffsetYScaleEnv = "DA_SHADOW_OFFSET_Y_SCALE";
        private const string ShadowNearAlphaScaleEnv = "DA_SHADOW_NEAR_ALPHA_SCALE";
        private const string ShadowFarAlphaScaleEnv = "DA_SHADOW_FAR_ALPHA_SCALE";
        private const string LayerBlurGammaEnv = "DA_LAYER_BLUR_GAMMA";
        private const string LayerBlurRadiusScaleEnv = "DA_LAYER_BLUR_RADIUS_SCALE";
        private const string LayerBlurStartRadiusScaleEnv = "DA_LAYER_BLUR_START_RADIUS_SCALE";
        private const string LayerBlurRampScaleEnv = "DA_LAYER_BLUR_RAMP_SCALE";
        private const string LayerBlurBiasEnv = "DA_LAYER_BLUR_BIAS";
        private const string LayerBlurUvBlendEnv = "DA_LAYER_BLUR_UV_BLEND";
        private const string LayerBlurRadiusCurveEnv = "DA_LAYER_BLUR_RADIUS_CURVE";
        private const string LayerBlurSrgbEnv = "DA_LAYER_BLUR_SRGB";
        private const string LayerBlurVariableEnv = "DA_LAYER_BLUR_VARIABLE";
        private const string EnvEnabledNumericValue = "1";
        private const string EnvEnabledTextValue = "true";
        private const string BlendModeNormal = "NORMAL";
        private const string BlendModeScreen = "SCREEN";
        private const string BlendModeLinearDodge = "LINEAR_DODGE";
        private const string BlendModePlusLighter = "PLUS_LIGHTER";
        private const string BlendModeOverlay = "OVERLAY";
        private const string ProgressiveBlurType = "PROGRESSIVE";
        private const string StyledRoundedRectShaderMissingMessage = "Figmage styled rounded rect shader is missing.";
        private const string ShadowBlurShaderMissingMessage = "Figmage shadow blur shader is missing.";
        private const string ShadowCompositeShaderMissingMessage = "Figmage shadow composite shader is missing.";
        private const string BlueNoiseTextureMissingMessage = "Figmage blue noise texture is missing.";
        private const string MissingAbsoluteBoundingBoxError = "Node has no absoluteBoundingBox.";
        private const string EmptyNodeBoundsError = "Node bounds are empty.";
        private const string MissingSupportedPaintsError = "Node has no supported fill/stroke paints.";
        private const string UnsupportedSvgPathCommandMessagePrefix = "Unsupported SVG path command: ";
        private const string RenderFailedMessageFormat = "{0}: render failed. {1}";
        private const string PropShadowTexPrepare = "_ShadowTexPrepare";
        private const string PropSourceTex = "_SourceTex";
        private const string PropPrepareOffset = "_PrepareOffset";
        private const string PropOverflowAlpha = "_OverflowAlpha";
        private const string PropShadowAlphaMultiplier = "_ShadowAlphaMultiplier";
        private const string PropBodyTex = "_BodyTex";
        private const string PropShadowTex = "_ShadowTex";
        private const string PropShadowColor = "_ShadowColor";
        private const string PropCanvasSize = "_CanvasSize";
        private const string PropShadowOffset = "_ShadowOffset";
        private const string PropShadowRect = "_ShadowRect";
        private const string PropPreserveShadowUnderBody = "_PreserveShadowUnderBody";
        private const string PropFillOpacityHint = "_FillOpacityHint";
        private const string PropLayerBlurMidATex = "_LayerBlurMidATex";
        private const string PropLayerBlurMidBTex = "_LayerBlurMidBTex";
        private const string PropLayerBlurMidCTex = "_LayerBlurMidCTex";
        private const string PropLayerBlurMidDTex = "_LayerBlurMidDTex";
        private const string PropLayerBlurMidETex = "_LayerBlurMidETex";
        private const string PropLayerBlurMidFTex = "_LayerBlurMidFTex";
        private const string PropShapeSize = "_ShapeSize";
        private const string PropShapeOffset = "_ShapeOffset";
        private const string PropShapeRotation = "_ShapeRotation";
        private const string PropLayerBlur = "_LayerBlur";
        private const string PropLayerBlurRamp = "_LayerBlurRamp";
        private const string PropLayerBlurOffsets = "_LayerBlurOffsets";
        private const string PropTapCount = "_TapCount";
        private const string PropWeights = "_Weights";
        private const string PropOffsets = "_Offsets";
        private const string PropBlueNoise = "_BlueNoise";
        private const string PropBlueNoiseTexelSize = "_BlueNoise_TexelSize";
        private const string PropTargetSize = "_TargetSize";
        private const string PropBlueNoiseStrength = "_BlueNoiseStrength";
        private const string PropProgressiveLayerBlur = "_ProgressiveLayerBlur";
        private const string PropBlurSrgb = "_BlurSrgb";
        private const string PropSourceTexelSize = "_SourceTex_TexelSize";
        private const string PropBlurAxis = "_BlurAxis";
        private const string PropPremultiplyInput = "_PremultiplyInput";
        private const string PropUnpremultiplyOutput = "_UnpremultiplyOutput";
        private const string PropDiamondExponent = "_DiamondExponent";
        private const string PropStrokeAngularOffset = "_StrokeAngularOffset";
        private const string PropCornerRadii = "_CornerRadii";
        private const string PropCornerSmoothing = "_CornerSmoothing";
        private const string PropShapeOpacity = "_ShapeOpacity";
        private const string PropStrokeWeight = "_StrokeWeight";
        private const string PropStrokeAlign = "_StrokeAlign";
        private const string PropFillExtendsUnderStroke = "_FillExtendsUnderStroke";
        private const string PropAntialiasing = "_Antialiasing";
        private const string PropRenderMode = "_RenderMode";
        private const string PropFillCount = "_FillCount";
        private const string PropStrokeCount = "_StrokeCount";
        private const int ShadowPreparePassIndex = 0;
        private const int ShadowAccumulatePassIndex = 2;
        private const int ShadowCompositePassIndex = 3;
        private const int LayerBlurCompositePassIndex = 4;
        private const int ProgressiveBlurKernelTapCount = 0;
        private const string PropDropShadow = "_DropShadow";
        private const string PropDropShadowOffset = "_DropShadowOffset";
        private const string PropDropShadowColor = "_DropShadowColor";
        private const string PropInnerShadow = "_InnerShadow";
        private const string PropInnerShadowOffset = "_InnerShadowOffset";
        private const string PropInnerShadowColor = "_InnerShadowColor";
        private const string PropInputBodyTex = "_InputBodyTex";
        private const string PropUseInputBodyTex = "_UseInputBodyTex";
        private const string PropBodyCompositeMode = "_BodyCompositeMode";
        private const string PropBodyDisjointComposite = "_BodyDisjointComposite";
        private const string PropLayerGradientTex = "_LayerGradientTex";
        private const string PropUseLayerGradientTex = "_UseLayerGradientTex";
        private const string PropPathPointCount = "_PathPointCount";
        private const string PropPathPoints = "_PathPoints";
        private const string PropStrokePathPointCount = "_StrokePathPointCount";
        private const string PropStrokePathPoints = "_StrokePathPoints";
        private const string FillStopPropertyFormat = "_FillStop{0}_{1}";
        private const string FillColorPropertyFormat = "_FillColor{0}_{1}";
        private const string StrokeStopPropertyFormat = "_StrokeStop{0}_{1}";
        private const string StrokeColorPropertyFormat = "_StrokeColor{0}_{1}";
        private const int MaxFillLayers = 3;
        private const int MaxStrokeLayers = 2;
        private const int MaxStops = 5;
        private const int MaxPathShaderPoints = 512;
        private const int MaxShadowKernelTaps = 64;
        private const int MaxShadowKernelExtent = 126;
        private const int LineSampleSteps = 1;
        private const int LayerKindSolid = 0;
        private const int LayerKindLinearGradient = 1;
        private const int LayerKindRadialGradient = 2;
        private const int LayerKindAngularGradient = 3;
        private const int LayerKindDiamondGradient = 4;
        private const int BlendModeIndexNormal = 0;
        private const int BlendModeIndexScreen = 1;
        private const int BlendModeIndexLinearDodge = 2;
        private const int BlendModeIndexOverlay = 3;
        private const int LayerBlurTuningModeDefault = 0;
        private const int LayerBlurTuningModeShadow = 1;
        private const int LayerBlurTuningModeOverlay = 2;
        private const int DefaultCubicSampleSteps = 36;
        private const int MinCubicSampleSteps = 4;
        private const int ArcPointEstimateSamples = 96;
        private const float DefaultShadowSigmaScale = 1.15f;
        private const float StrokeAlignInsideValue = 0f;
        private const float StrokeAlignCenterValue = 1f;
        private const float StrokeAlignOutsideValue = 2f;
        private const float ShadowBlurRatioEpsilon = 1e-5f;
        private const float PreserveShadowStartOpacityWithStroke = 0.75f;
        private const float PreserveShadowStartOpacityWithoutStroke = 0.85f;
        private const float PreserveShadowEndOpacity = 0.95f;
        private const float LayerBlurMinimumScale = 0.1f;
        private const float LayerBlurGammaDefault = 0.94f;
        private const float LayerBlurRadiusScaleDefault = 0.95f;
        private const float LayerBlurStartRadiusScaleDefault = 0.7f;
        private const float LayerBlurRampScaleTuningModeOverlay = 0.7f;
        private const float LayerBlurRampScaleTuningModeShadow = 1.7f;
        private const float LayerBlurRampScaleDefault = 0.9f;
        private const float LayerBlurBiasTuningModeShadow = 0.08f;
        private const float LayerBlurBiasDefault = 0.04f;
        private const float LayerBlurUvBlendTuningModeShadow = 1.4f;
        private const float LayerBlurRadiusCurveTuningModeOverlay = 0.4f;
        private const float LayerBlurRadiusCurveTuningModeShadow = 1.5f;
        private const float LayerBlurSrgbThreshold = 0.5f;
        private const float ProgressiveBlurOffsetEpsilonSqr = 1e-6f;
        private const float GaussianSqrtHalf = 0.7071067811865476f;
        private const float GaussianSigmaFloor = 1e-5f;
        private const float GaussianAnalyticalThreshold = 0.8f;
        private const float GaussianPackedWeightEpsilon = 1e-6f;
        private const float ErfA1 = 0.254829592f;
        private const float ErfA2 = -0.284496736f;
        private const float ErfA3 = 1.421413741f;
        private const float ErfA4 = -1.453152027f;
        private const float ErfA5 = 1.061405429f;
        private const float ErfP = 0.3275911f;
        private const float NegativeSpreadScaleNear = 0.5f;
        private const float NegativeSpreadScaleFar = 0.85f;
        private const float NegativeSpreadBlurNear = 30f;
        private const float NegativeSpreadBlurFar = 50f;
        private const float AdaptiveShadowSigmaScale = 1f;
        private const float AdaptiveShadowSpreadScale = 1f;
        private const float StrokeAlignOutsideThreshold = 1.5f;
        private const float StrokeAlignCenterThreshold = 0.5f;
        private const float SecondaryScreenStrokeOpacityScale = 0.30f;
        private const float OpposingShadowOffsetEpsilon = 1e-3f;
        private const float GradientDeterminantEpsilon = 1e-5f;
        private const float ArcDistanceEpsilon = 1e-4f;
        private const float ArcComputationEpsilon = 1e-6f;
        private const float ArcSamplingResolution = 8f;
        private const float PointEqualityEpsilonSqr = 1e-8f;
        private static readonly string[] FillLayerNames = { "_FillLayer0", "_FillLayer1", "_FillLayer2" };
        private static readonly string[] StrokeLayerNames = { "_StrokeLayer0", "_StrokeLayer1" };
        private static readonly string[] FillTransformANames = { "_FillTransformA0", "_FillTransformA1", "_FillTransformA2" };
        private static readonly string[] FillTransformBNames = { "_FillTransformB0", "_FillTransformB1", "_FillTransformB2" };
        private static readonly string[] StrokeTransformANames = { "_StrokeTransformA0", "_StrokeTransformA1" };
        private static readonly string[] StrokeTransformBNames = { "_StrokeTransformB0", "_StrokeTransformB1" };
        private static readonly float[] ProgressiveBlurSampleFractions = { 1f / 7f, 2f / 7f, 3f / 7f, 4f / 7f, 5f / 7f, 6f / 7f };
        private static readonly int[] CubicSampleStepCandidates = { 64, 56, 48, 44, 40, 36, 32, 28, 24, 20, 16, 12, 8, 6, 4 };
        private static readonly float FullRotationRadians = Mathf.PI * 2f;

        Material _bodyMaterial;
        Material _blurMaterial;
        Material _compositeMaterial;
        readonly Texture2D shadowBlueNoiseTexture;
        readonly Vector4[] _pathPointBuffer = new Vector4[MaxPathShaderPoints];
        readonly Vector4[] _strokePathPointBuffer = new Vector4[MaxPathShaderPoints];
        readonly float[] _shadowKernelWeightBuffer = new float[MaxShadowKernelTaps];
        readonly float[] _shadowKernelOffsetBuffer = new float[MaxShadowKernelTaps];
        float? _shadowSigmaScaleCache;
        float? _shadowSpreadScaleCache;
        float? _shadowAlphaScaleCache;
        float? _shadowOffsetXScaleCache;
        float? _shadowOffsetYScaleCache;
        float? _shadowNearAlphaScaleCache;
        float? _shadowFarAlphaScaleCache;
        public FigmageRenderer(Shader bodyShader = null, Shader blurShader = null, Shader compositeShader = null, Texture2D blueNoiseTexture = null)
        {
            bodyShader ??= Resources.Load<Shader>("Figmage/Shaders/FigmageStyledRoundedRect");
            blurShader ??= Resources.Load<Shader>("Figmage/Shaders/FigmageShadowBlur");
            compositeShader ??= Resources.Load<Shader>("Figmage/Shaders/FigmageShadowComposite");
            shadowBlueNoiseTexture = blueNoiseTexture ?? Resources.Load<Texture2D>("Figmage/Textures/Blue Noise Tile 64");

            if (bodyShader == null)
                throw new InvalidOperationException(StyledRoundedRectShaderMissingMessage);
            if (blurShader == null)
                throw new InvalidOperationException(ShadowBlurShaderMissingMessage);
            if (compositeShader == null)
                throw new InvalidOperationException(ShadowCompositeShaderMissingMessage);
            if (shadowBlueNoiseTexture == null)
                throw new InvalidOperationException(BlueNoiseTextureMissingMessage);

            _bodyMaterial = new Material(bodyShader) { hideFlags = HideFlags.HideAndDontSave };
            _blurMaterial = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };
            _compositeMaterial = new Material(compositeShader) { hideFlags = HideFlags.HideAndDontSave };
        }

        public void Dispose()
        {
            DestroyUnityObject(_bodyMaterial);
            DestroyUnityObject(_blurMaterial);
            DestroyUnityObject(_compositeMaterial);
        }

        public void RenderToPng(FigmageNode node, float scale, string outputPath)
        {
            RenderToPng(node, scale, outputPath, null);
        }

        public void RenderToPng(FigmageNode node, float scale, string outputPath, Rect? canvasBoundsOverride)
        {
            ExportEntry entry = CreateRenderEntry(node, scale, outputPath, canvasBoundsOverride);
            RenderVariant(outputPath, entry, scale, true);
        }

        public byte[] RenderToPngBytes(FigmageNode node, float scale, string outputPath)
        {
            return RenderToPngBytes(node, scale, outputPath, null);
        }

        public byte[] RenderToPngBytes(FigmageNode node, float scale, string outputPath, Rect? canvasBoundsOverride)
        {
            ExportEntry entry = CreateRenderEntry(node, scale, outputPath, canvasBoundsOverride);
            return RenderVariant(outputPath, entry, scale, false);
        }

        static ExportEntry CreateRenderEntry(FigmageNode node, float scale, string outputPath, Rect? canvasBoundsOverride)
        {
            ExportEntry entry = new ExportEntry
            {
                Node = node,
                NodeId = node.Id,
                NodeName = node.Name,
                OutputFileName = Path.GetFileName(outputPath)
            };

            if (canvasBoundsOverride.HasValue)
            {
                Rect canvasBounds = canvasBoundsOverride.Value;
                entry.UseExternalCanvasBounds = true;
                entry.CanvasBounds = new FigmageRect
                {
                    X = canvasBounds.x,
                    Y = canvasBounds.y,
                    Width = canvasBounds.width,
                    Height = canvasBounds.height
                };

                if (node.AbsoluteBoundingBox.Width > 0f &&
                    node.AbsoluteBoundingBox.Height > 0f)
                {
                    entry.ContentBounds = new FigmageRect
                    {
                        X = node.AbsoluteBoundingBox.X,
                        Y = node.AbsoluteBoundingBox.Y,
                        Width = node.AbsoluteBoundingBox.Width,
                        Height = node.AbsoluteBoundingBox.Height
                    };
                }

                entry.RenderWidth = Mathf.Max(1, Mathf.CeilToInt(canvasBounds.width * scale));
                entry.RenderHeight = Mathf.Max(1, Mathf.CeilToInt(canvasBounds.height * scale));
            }

            return entry;
        }

        sealed class ExportEntry
        {
            public string NodeId;
            public string NodeName;
            public string OutputFileName;
            public string RenderedPath;
            public int RenderWidth;
            public int RenderHeight;
            public string Error;
            public FigmageRect ContentBounds;
            public FigmageRect CanvasBounds;
            public bool UseExternalCanvasBounds;
            public FigmageNode Node;
        }

        sealed class ShadowRenderPlan
        {
            public ShadowSpec Shadow;
            public float BlurRadius;
            public float SpreadPx;
            public float MaskSpreadPx;
            public float PostBlurAlphaMultiplier;
            public Vector2 ScaledShadowOffset;
            public Vector2 ShadowCanvasSize;
            public Rect ShadowRect;
        }

        sealed class ShaderRenderSpec
        {
            public Vector2Int CanvasSize;
            public Vector2 ShapeSize;
            public Vector2 ShapeOffset;
            public Vector4 CornerRadii;
            public float CornerSmoothing;
            public float ShapeRotationRadians;
            public float ShapeOpacity;
            public float StrokeWeight;
            public float StrokeAlign;
            public float Antialiasing;
            public Vector2 SampledBoundsCenter;
            public Vector2 SampledBoundsSize;
            public Vector4[] FillPathPoints;
            public Vector4[] StrokePathPoints;
            public List<LayerSpec> Fills;
            public List<LayerSpec> Strokes;
            public List<ShadowSpec> DropShadows;
            public ShadowSpec InnerShadow;
            public LayerBlurSpec LayerBlur;
            public int LayerBlurTuningMode;
        }

        sealed class LayerSpec
        {
            public int Kind;
            public float Opacity;
            public int BlendMode;
            public List<StopSpec> Stops;
            public Vector3 TransformRowA;
            public Vector3 TransformRowB;
        }

        sealed class StopSpec
        {
            public float Position;
            public float Alpha;
            public Color Color;
        }

        readonly struct SvgCommand
        {
            public SvgCommand(char type, float[] values)
            {
                Type = type;
                Values = values;
            }

            public char Type { get; }
            public float[] Values { get; }
        }

        struct ShadowSpec
        {
            public static ShadowSpec Disabled => new ShadowSpec
            {
                Enabled = false,
                BlurRadius = 0f,
                Spread = 0f,
                Offset = Vector2.zero,
                BlendMode = 0,
                Color = Color.clear
            };

            public bool Enabled;
            public float BlurRadius;
            public float Spread;
            public Vector2 Offset;
            public int BlendMode;
            public Color Color;
        }

        struct LayerBlurSpec
        {
            public static LayerBlurSpec Disabled => new LayerBlurSpec
            {
                Enabled = false,
                Progressive = false,
                Radius = 0f,
                StartRadius = 0f,
                StartOffset = Vector2.zero,
                EndOffset = Vector2.right
            };

            public bool Enabled;
            public bool Progressive;
            public float Radius;
            public float StartRadius;
            public Vector2 StartOffset;
            public Vector2 EndOffset;
        }
    }
}
