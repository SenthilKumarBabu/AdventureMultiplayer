using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DA_Assets.Figmage
{
    sealed partial class FigmageRenderer
    {
        byte[] RenderVariant(string outputPath, ExportEntry entry, float exportScale, bool writeFile)
        {
            float renderScale = Mathf.Max(0.01f, exportScale);
            if (!TryBuildRenderSpec(entry.Node, renderScale, out ShaderRenderSpec spec, out string error))
            {
                if (error == MissingSupportedPaintsError && HasRenderableChildren(entry.Node))
                    return RenderChildrenVariant(outputPath, entry, renderScale, writeFile);

                throw new InvalidOperationException(error);
            }

            if (entry.RenderWidth > 0 && entry.RenderHeight > 0)
            {
                spec.CanvasSize = new Vector2Int(entry.RenderWidth, entry.RenderHeight);
                UpdateShapeOffsetForCanvas(spec, entry, renderScale);
            }

            if (writeFile)
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
            }

            RenderTexture bodyRt = null;
            RenderTexture shadowMaskRt = null;
            RenderTexture shadowCutoutMaskRt = null;
            RenderTexture blurRtA = null;
            RenderTexture blurRtB = null;
            RenderTexture preparedShadowRt = null;
            RenderTexture shadowAccumRtA = null;
            RenderTexture shadowAccumRtB = null;
            RenderTexture finalRt = null;
            RenderTexture layerBlurTempRt = null;
            RenderTexture layerBlurTargetRt = null;
            RenderTexture layerBlurOutputRt = null;
            RenderTexture layerBlurStartTempRt = null;
            RenderTexture layerBlurStartTargetRt = null;
            RenderTexture layerBlurMidATempRt = null;
            RenderTexture layerBlurMidATargetRt = null;
            RenderTexture layerBlurMidBTempRt = null;
            RenderTexture layerBlurMidBTargetRt = null;
            RenderTexture layerBlurMidCTempRt = null;
            RenderTexture layerBlurMidCTargetRt = null;
            RenderTexture layerBlurMidDTempRt = null;
            RenderTexture layerBlurMidDTargetRt = null;
            RenderTexture layerBlurMidETempRt = null;
            RenderTexture layerBlurMidETargetRt = null;
            RenderTexture layerBlurMidFTempRt = null;
            RenderTexture layerBlurMidFTargetRt = null;

            try
            {
                bodyRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                finalRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);

                if (RequiresLayeredBodyRendering(spec))
                {
                    RenderLayeredBody(spec, bodyRt);
                }
                else
                {
                    ConfigureBodyMaterial(_bodyMaterial, spec, false, spec.ShapeOffset, ShadowSpec.Disabled);
                    Graphics.Blit(Texture2D.blackTexture, bodyRt, _bodyMaterial);
                }

                if (spec.DropShadows.Count > 0)
                {
                    float maxBlurRadius = spec.DropShadows.Max(x => x.BlurRadius);
                    float shadowStrokeExpand = spec.Strokes.Count > 0
                        ? GetOuterStrokeExpansion(spec.StrokeAlign, spec.StrokeWeight)
                        : 0f;
                    Vector2 shadowMaskShapeOffset = spec.ShapeOffset;
                    List<ShadowRenderPlan> shadowPlans = BuildShadowPlans(spec, entry, shadowStrokeExpand);
                    shadowAccumRtA = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                    shadowAccumRtB = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                    ClearRenderTexture(shadowAccumRtA);
                    ClearRenderTexture(shadowAccumRtB);
                    RenderTexture shadowAccumSourceRt = shadowAccumRtA;
                    RenderTexture shadowAccumTargetRt = shadowAccumRtB;

                    for (int shadowIndex = 0; shadowIndex < shadowPlans.Count; shadowIndex++)
                    {
                        ShadowRenderPlan plan = shadowPlans[shadowIndex];
                        ShadowSpec shadow = plan.Shadow;

                        shadowMaskRt = CreateRenderTexture(
                            Mathf.Max(1, Mathf.CeilToInt(plan.ShadowCanvasSize.x)),
                            Mathf.Max(1, Mathf.CeilToInt(plan.ShadowCanvasSize.y)));
                        shadowCutoutMaskRt = CreateRenderTexture(shadowMaskRt.width, shadowMaskRt.height);
                        blurRtA = CreateRenderTexture(shadowMaskRt.width, shadowMaskRt.height);
                        blurRtB = CreateRenderTexture(shadowMaskRt.width, shadowMaskRt.height);
                        preparedShadowRt = CreateRenderTexture(shadowMaskRt.width, shadowMaskRt.height);

                        ConfigureShadowMaskMaterial(_bodyMaterial, spec, shadowMaskRt.width, shadowMaskRt.height, shadowMaskShapeOffset, plan.MaskSpreadPx);
                        Graphics.Blit(Texture2D.blackTexture, shadowMaskRt, _bodyMaterial);
                        ConfigureShadowMaskMaterial(_bodyMaterial, spec, shadowCutoutMaskRt.width, shadowCutoutMaskRt.height, shadowMaskShapeOffset, 0f);
                        Graphics.Blit(Texture2D.blackTexture, shadowCutoutMaskRt, _bodyMaterial);

                        BlurTexture(shadowMaskRt, blurRtA, blurRtB, plan.BlurRadius, false);

                        _compositeMaterial.SetTexture(PropShadowTexPrepare, blurRtB);
                        _compositeMaterial.SetTexture(PropSourceTex, shadowCutoutMaskRt);
                        _compositeMaterial.SetVector(PropPrepareOffset, Vector4.zero);
                        _compositeMaterial.SetFloat(PropOverflowAlpha, 0f);
                        _compositeMaterial.SetFloat(PropShadowAlphaMultiplier, plan.PostBlurAlphaMultiplier);
                        Graphics.Blit(Texture2D.blackTexture, preparedShadowRt, _compositeMaterial, ShadowPreparePassIndex);

                        Color shadowColor = shadow.Color;
                        float blurT = maxBlurRadius > ShadowBlurRatioEpsilon ? Mathf.Clamp01(shadow.BlurRadius / maxBlurRadius) : 1f;
                        float perLayerAlphaScale = Mathf.Lerp(ResolveShadowNearAlphaScale(), ResolveShadowFarAlphaScale(), blurT);
                        shadowColor.a = Mathf.Clamp01(shadowColor.a * ResolveShadowAlphaScale() * perLayerAlphaScale * spec.ShapeOpacity);
                        _compositeMaterial.SetTexture(PropBodyTex, shadowAccumSourceRt);
                        _compositeMaterial.SetTexture(PropShadowTex, preparedShadowRt);
                        _compositeMaterial.SetVector(PropShadowColor, shadowColor);
                        _compositeMaterial.SetVector(PropCanvasSize, new Vector4(spec.CanvasSize.x, spec.CanvasSize.y, 0f, 0f));
                        _compositeMaterial.SetVector(PropShadowOffset, new Vector4(
                            plan.ScaledShadowOffset.x,
                            -plan.ScaledShadowOffset.y,
                            0f,
                            0f));
                        _compositeMaterial.SetVector(PropShadowRect, new Vector4(
                            plan.ShadowRect.x,
                            plan.ShadowRect.y,
                            plan.ShadowRect.width,
                            plan.ShadowRect.height));
                        Graphics.Blit(Texture2D.blackTexture, shadowAccumTargetRt, _compositeMaterial, ShadowAccumulatePassIndex);

                        RenderTexture nextAccum = shadowAccumTargetRt;
                        shadowAccumTargetRt = shadowAccumSourceRt;
                        shadowAccumSourceRt = nextAccum;

                        ReleaseTemporary(shadowMaskRt);
                        ReleaseTemporary(shadowCutoutMaskRt);
                        ReleaseTemporary(blurRtA);
                        ReleaseTemporary(blurRtB);
                        ReleaseTemporary(preparedShadowRt);
                        shadowMaskRt = null;
                        shadowCutoutMaskRt = null;
                        blurRtA = null;
                        blurRtB = null;
                        preparedShadowRt = null;
                    }

                    _compositeMaterial.SetTexture(PropBodyTex, bodyRt);
                    _compositeMaterial.SetTexture(PropShadowTex, shadowAccumSourceRt);
                    float preserveShadowUnderBody = 0f;
                    float fillOpacityHint = 0f;
                    if (spec.Fills.Count == 1 && spec.Fills[0].BlendMode != BlendModeIndexNormal)
                    {
                        fillOpacityHint = Mathf.Clamp01(spec.ShapeOpacity * spec.Fills[0].Opacity);
                        float preserveStartOpacity = spec.Strokes.Count > 0 ? PreserveShadowStartOpacityWithStroke : PreserveShadowStartOpacityWithoutStroke;
                        preserveShadowUnderBody = Mathf.InverseLerp(preserveStartOpacity, PreserveShadowEndOpacity, spec.Fills[0].Opacity);
                        preserveShadowUnderBody = Mathf.Clamp01(preserveShadowUnderBody);
                    }

                    _compositeMaterial.SetFloat(PropPreserveShadowUnderBody, preserveShadowUnderBody);
                    _compositeMaterial.SetFloat(PropFillOpacityHint, fillOpacityHint);
                    _compositeMaterial.SetVector(PropShadowOffset, Vector4.zero);
                    Graphics.Blit(Texture2D.blackTexture, finalRt, _compositeMaterial, ShadowCompositePassIndex);
                }
                else
                {
                    Graphics.Blit(bodyRt, finalRt);
                }

                if (spec.LayerBlur.Enabled && spec.LayerBlur.Radius > 0f)
                {
                    layerBlurTempRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                    layerBlurTargetRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                    BlurTexture(finalRt, layerBlurTempRt, layerBlurTargetRt, spec.LayerBlur.Radius, true);

                    if (spec.LayerBlur.Progressive)
                    {
                        layerBlurOutputRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                        RenderTexture progressiveStartRt = finalRt;
                        float startRadius = Mathf.Min(spec.LayerBlur.StartRadius, spec.LayerBlur.Radius);
                        if (ResolveLayerBlurVariable())
                        {
                            BlurTextureProgressive(finalRt, layerBlurTempRt, layerBlurOutputRt, spec);
                        }
                        else
                        {
                            if (spec.LayerBlur.StartRadius > 0f)
                            {
                                layerBlurStartTempRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                                layerBlurStartTargetRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                                BlurTexture(finalRt, layerBlurStartTempRt, layerBlurStartTargetRt, startRadius, true);
                                progressiveStartRt = layerBlurStartTargetRt;
                            }

                            float radiusCurve = ResolveLayerBlurRadiusCurve(spec);
                            float midARadius = InterpolateProgressiveRadius(startRadius, spec.LayerBlur.Radius, ProgressiveBlurSampleFractions[0], radiusCurve);
                            float midBRadius = InterpolateProgressiveRadius(startRadius, spec.LayerBlur.Radius, ProgressiveBlurSampleFractions[1], radiusCurve);
                            float midCRadius = InterpolateProgressiveRadius(startRadius, spec.LayerBlur.Radius, ProgressiveBlurSampleFractions[2], radiusCurve);
                            float midDRadius = InterpolateProgressiveRadius(startRadius, spec.LayerBlur.Radius, ProgressiveBlurSampleFractions[3], radiusCurve);
                            float midERadius = InterpolateProgressiveRadius(startRadius, spec.LayerBlur.Radius, ProgressiveBlurSampleFractions[4], radiusCurve);
                            float midFRadius = InterpolateProgressiveRadius(startRadius, spec.LayerBlur.Radius, ProgressiveBlurSampleFractions[5], radiusCurve);
                            layerBlurMidATempRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidATargetRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidBTempRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidBTargetRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidCTempRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidCTargetRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidDTempRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidDTargetRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidETempRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidETargetRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidFTempRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            layerBlurMidFTargetRt = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                            BlurTexture(finalRt, layerBlurMidATempRt, layerBlurMidATargetRt, midARadius, true);
                            BlurTexture(finalRt, layerBlurMidBTempRt, layerBlurMidBTargetRt, midBRadius, true);
                            BlurTexture(finalRt, layerBlurMidCTempRt, layerBlurMidCTargetRt, midCRadius, true);
                            BlurTexture(finalRt, layerBlurMidDTempRt, layerBlurMidDTargetRt, midDRadius, true);
                            BlurTexture(finalRt, layerBlurMidETempRt, layerBlurMidETargetRt, midERadius, true);
                            BlurTexture(finalRt, layerBlurMidFTempRt, layerBlurMidFTargetRt, midFRadius, true);

                            _compositeMaterial.SetTexture(PropBodyTex, progressiveStartRt);
                            _compositeMaterial.SetTexture(PropLayerBlurMidATex, layerBlurMidATargetRt);
                            _compositeMaterial.SetTexture(PropLayerBlurMidBTex, layerBlurMidBTargetRt);
                            _compositeMaterial.SetTexture(PropLayerBlurMidCTex, layerBlurMidCTargetRt);
                            _compositeMaterial.SetTexture(PropLayerBlurMidDTex, layerBlurMidDTargetRt);
                            _compositeMaterial.SetTexture(PropLayerBlurMidETex, layerBlurMidETargetRt);
                            _compositeMaterial.SetTexture(PropLayerBlurMidFTex, layerBlurMidFTargetRt);
                            _compositeMaterial.SetTexture(PropShadowTex, layerBlurTargetRt);
                            _compositeMaterial.SetVector(PropCanvasSize, new Vector4(spec.CanvasSize.x, spec.CanvasSize.y, 0f, 0f));
                            _compositeMaterial.SetVector(PropShapeSize, new Vector4(spec.ShapeSize.x, spec.ShapeSize.y, 0f, 0f));
                            _compositeMaterial.SetVector(PropShapeOffset, new Vector4(spec.ShapeOffset.x, spec.ShapeOffset.y, 0f, 0f));
                            _compositeMaterial.SetFloat(PropShapeRotation, spec.ShapeRotationRadians);
                            _compositeMaterial.SetVector(PropLayerBlur, new Vector4(startRadius, spec.LayerBlur.Radius, ResolveLayerBlurGamma(), 0f));
                            _compositeMaterial.SetVector(PropLayerBlurRamp, new Vector4(ResolveLayerBlurRampScale(spec), ResolveLayerBlurBias(spec), ResolveLayerBlurUvBlend(spec), 0f));
                            _compositeMaterial.SetVector(PropLayerBlurOffsets, new Vector4(
                                spec.LayerBlur.StartOffset.x,
                                spec.LayerBlur.StartOffset.y,
                                spec.LayerBlur.EndOffset.x,
                                spec.LayerBlur.EndOffset.y));
                            Graphics.Blit(Texture2D.blackTexture, layerBlurOutputRt, _compositeMaterial, LayerBlurCompositePassIndex);
                        }
                    }
                    else
                    {
                        layerBlurOutputRt = layerBlurTargetRt;
                        layerBlurTargetRt = null;
                    }

                    ReleaseTemporary(finalRt);
                    finalRt = layerBlurOutputRt;
                    layerBlurOutputRt = null;
                }

                byte[] pngBytes = EncodeRenderTexturePng(finalRt);

                if (writeFile)
                    File.WriteAllBytes(outputPath, pngBytes);

                entry.RenderedPath = outputPath;
                return pngBytes;
            }
            catch (Exception ex)
            {
                entry.Error = ex.Message;
                Debug.LogError(string.Format(CultureInfo.InvariantCulture, RenderFailedMessageFormat, entry.NodeName, ex.Message));
                throw;
            }
            finally
            {
                ReleaseTemporary(bodyRt);
                ReleaseTemporary(shadowMaskRt);
                ReleaseTemporary(shadowCutoutMaskRt);
                ReleaseTemporary(blurRtA);
                ReleaseTemporary(blurRtB);
                ReleaseTemporary(preparedShadowRt);
                ReleaseTemporary(shadowAccumRtA);
                ReleaseTemporary(shadowAccumRtB);
                ReleaseTemporary(finalRt);
                ReleaseTemporary(layerBlurTempRt);
                ReleaseTemporary(layerBlurTargetRt);
                ReleaseTemporary(layerBlurOutputRt);
                ReleaseTemporary(layerBlurStartTempRt);
                ReleaseTemporary(layerBlurStartTargetRt);
                ReleaseTemporary(layerBlurMidATempRt);
                ReleaseTemporary(layerBlurMidATargetRt);
                ReleaseTemporary(layerBlurMidBTempRt);
                ReleaseTemporary(layerBlurMidBTargetRt);
                ReleaseTemporary(layerBlurMidCTempRt);
                ReleaseTemporary(layerBlurMidCTargetRt);
                ReleaseTemporary(layerBlurMidDTempRt);
                ReleaseTemporary(layerBlurMidDTargetRt);
                ReleaseTemporary(layerBlurMidETempRt);
                ReleaseTemporary(layerBlurMidETargetRt);
                ReleaseTemporary(layerBlurMidFTempRt);
                ReleaseTemporary(layerBlurMidFTargetRt);
            }
        }

        byte[] RenderChildrenVariant(string outputPath, ExportEntry entry, float renderScale, bool writeFile)
        {
            Rect canvasBounds = ResolveContainerCanvasBounds(entry);
            int width = entry.RenderWidth > 0 ? entry.RenderWidth : Mathf.Max(1, Mathf.CeilToInt(canvasBounds.width * renderScale));
            int height = entry.RenderHeight > 0 ? entry.RenderHeight : Mathf.Max(1, Mathf.CeilToInt(canvasBounds.height * renderScale));
            Texture2D accumulated = new Texture2D(width, height, TextureFormat.RGBA32, false, true);

            try
            {
                Color32[] pixels = new Color32[width * height];
                bool hasRenderedChild = false;

                for (int i = 0; i < entry.Node.Children.Count; i++)
                {
                    FigmageNode child = entry.Node.Children[i];
                    if (!HasRenderableNode(child))
                        continue;

                    ExportEntry childEntry = CreateRenderEntry(child, renderScale, string.Empty, canvasBounds);
                    childEntry.RenderWidth = width;
                    childEntry.RenderHeight = height;
                    byte[] childPng = RenderVariant(string.Empty, childEntry, renderScale, false);
                    Texture2D childTexture = DecodePng(childPng);
                    try
                    {
                        if (childTexture.width != width || childTexture.height != height)
                            throw new InvalidOperationException("Child render size does not match container size.");

                        CompositeOver(pixels, childTexture.GetPixels32());
                        hasRenderedChild = true;
                    }
                    finally
                    {
                        DestroyUnityObject(childTexture);
                    }
                }

                if (!hasRenderedChild)
                    throw new InvalidOperationException(MissingSupportedPaintsError);

                accumulated.SetPixels32(pixels);
                accumulated.Apply(false, false);
                byte[] pngBytes = accumulated.EncodeToPNG();

                if (writeFile)
                {
                    string directory = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);
                    File.WriteAllBytes(outputPath, pngBytes);
                }

                entry.RenderedPath = outputPath;
                return pngBytes;
            }
            finally
            {
                DestroyUnityObject(accumulated);
            }
        }

        bool RequiresLayeredBodyRendering(ShaderRenderSpec spec)
        {
            return spec.Fills.Count > MaxFillLayers ||
                   spec.Strokes.Count > MaxStrokeLayers ||
                   HasGradientWithManyStops(spec.Fills) ||
                   HasGradientWithManyStops(spec.Strokes);
        }

        static bool HasGradientWithManyStops(List<LayerSpec> layers)
        {
            if (layers == null)
                return false;

            for (int i = 0; i < layers.Count; i++)
            {
                LayerSpec layer = layers[i];
                if (layer.Kind != LayerKindSolid && layer.Stops != null && layer.Stops.Count > MaxStops)
                    return true;
            }

            return false;
        }

        void RenderLayeredBody(ShaderRenderSpec spec, RenderTexture destination)
        {
            RenderTexture source = null;
            RenderTexture target = null;

            try
            {
                source = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                target = CreateRenderTexture(spec.CanvasSize.x, spec.CanvasSize.y);
                ClearRenderTexture(source);

                bool hasAccumulatedBody = false;

                for (int i = 0; i < spec.Fills.Count; i++)
                {
                    RenderSingleBodyLayer(spec, spec.Fills[i], true, spec.Fills[i].BlendMode, false, ref source, ref target, ref hasAccumulatedBody);
                }

                for (int i = 0; i < spec.Strokes.Count; i++)
                {
                    int blendMode = i == 0 ? BlendModeIndexNormal : spec.Strokes[i].BlendMode;
                    bool disjoint = spec.StrokeAlign > StrokeAlignOutsideThreshold;
                    RenderSingleBodyLayer(spec, spec.Strokes[i], false, blendMode, disjoint, ref source, ref target, ref hasAccumulatedBody);
                }

                if (spec.InnerShadow.Enabled)
                {
                    RenderInnerShadowBodyPass(spec, ref source, ref target, ref hasAccumulatedBody);
                }

                if (hasAccumulatedBody)
                {
                    Graphics.Blit(source, destination);
                }
                else
                {
                    ClearRenderTexture(destination);
                }
            }
            finally
            {
                ReleaseTemporary(source);
                ReleaseTemporary(target);
            }
        }

        void RenderSingleBodyLayer(
            ShaderRenderSpec sourceSpec,
            LayerSpec layer,
            bool fillLayer,
            int compositeMode,
            bool disjointComposite,
            ref RenderTexture source,
            ref RenderTexture target,
            ref bool hasAccumulatedBody)
        {
            ShaderRenderSpec layerSpec = CreateBodyLayerSpec(sourceSpec, fillLayer ? layer : null, fillLayer ? null : layer, ShadowSpec.Disabled);
            Texture2D gradientTexture = null;

            try
            {
                ConfigureBodyMaterial(_bodyMaterial, layerSpec, false, sourceSpec.ShapeOffset, ShadowSpec.Disabled);
                _bodyMaterial.SetTexture(PropInputBodyTex, hasAccumulatedBody ? source : Texture2D.blackTexture);
                _bodyMaterial.SetFloat(PropUseInputBodyTex, hasAccumulatedBody ? 1f : 0f);
                _bodyMaterial.SetFloat(PropBodyCompositeMode, compositeMode);
                _bodyMaterial.SetFloat(PropBodyDisjointComposite, disjointComposite ? 1f : 0f);

                if (layer.Kind != LayerKindSolid)
                {
                    gradientTexture = CreateGradientTexture(layer);
                    _bodyMaterial.SetTexture(PropLayerGradientTex, gradientTexture);
                    _bodyMaterial.SetFloat(PropUseLayerGradientTex, 1f);
                }

                Graphics.Blit(Texture2D.blackTexture, target, _bodyMaterial);
            }
            finally
            {
                DestroyUnityObject(gradientTexture);
            }

            RenderTexture nextSource = target;
            target = source;
            source = nextSource;
            hasAccumulatedBody = true;
        }

        void RenderInnerShadowBodyPass(
            ShaderRenderSpec sourceSpec,
            ref RenderTexture source,
            ref RenderTexture target,
            ref bool hasAccumulatedBody)
        {
            ShaderRenderSpec layerSpec = CreateBodyLayerSpec(sourceSpec, null, null, sourceSpec.InnerShadow);
            ConfigureBodyMaterial(_bodyMaterial, layerSpec, false, sourceSpec.ShapeOffset, ShadowSpec.Disabled);
            _bodyMaterial.SetTexture(PropInputBodyTex, hasAccumulatedBody ? source : Texture2D.blackTexture);
            _bodyMaterial.SetFloat(PropUseInputBodyTex, hasAccumulatedBody ? 1f : 0f);
            _bodyMaterial.SetFloat(PropBodyCompositeMode, BlendModeIndexNormal);
            _bodyMaterial.SetFloat(PropBodyDisjointComposite, 0f);

            Graphics.Blit(Texture2D.blackTexture, target, _bodyMaterial);

            RenderTexture nextSource = target;
            target = source;
            source = nextSource;
            hasAccumulatedBody = true;
        }

        static ShaderRenderSpec CreateBodyLayerSpec(ShaderRenderSpec source, LayerSpec fillLayer, LayerSpec strokeLayer, ShadowSpec innerShadow)
        {
            return new ShaderRenderSpec
            {
                CanvasSize = source.CanvasSize,
                ShapeSize = source.ShapeSize,
                ShapeOffset = source.ShapeOffset,
                CornerRadii = source.CornerRadii,
                CornerSmoothing = source.CornerSmoothing,
                ShapeRotationRadians = source.ShapeRotationRadians,
                ShapeOpacity = source.ShapeOpacity,
                StrokeWeight = source.StrokeWeight,
                StrokeAlign = source.StrokeAlign,
                Antialiasing = source.Antialiasing,
                SampledBoundsCenter = source.SampledBoundsCenter,
                SampledBoundsSize = source.SampledBoundsSize,
                FillPathPoints = source.FillPathPoints,
                StrokePathPoints = source.StrokePathPoints,
                Fills = fillLayer != null ? new List<LayerSpec> { fillLayer } : new List<LayerSpec>(),
                Strokes = strokeLayer != null ? new List<LayerSpec> { strokeLayer } : new List<LayerSpec>(),
                DropShadows = new List<ShadowSpec>(),
                InnerShadow = innerShadow,
                LayerBlur = LayerBlurSpec.Disabled,
                LayerBlurTuningMode = source.LayerBlurTuningMode
            };
        }

        static Texture2D CreateGradientTexture(LayerSpec layer)
        {
            int stopCount = layer.Stops?.Count ?? 0;
            int width = Mathf.Clamp(Mathf.Max(256, stopCount * 128), 256, 4096);
            Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false, true)
            {
                name = "Figmage Gradient",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[width];
            for (int i = 0; i < width; i++)
            {
                float position = width <= 1 ? 0f : i / (float)(width - 1);
                pixels[i] = EvaluateGradientTextureColor(layer, position);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Color EvaluateGradientTextureColor(LayerSpec layer, float position)
        {
            if (layer.Stops == null || layer.Stops.Count == 0)
                return Color.white;

            if (layer.Stops.Count == 1)
            {
                StopSpec only = layer.Stops[0];
                Color onlyColor = only.Color;
                onlyColor.a = only.Alpha;
                return onlyColor;
            }

            StopSpec previous = layer.Stops[0];
            StopSpec next = layer.Stops[layer.Stops.Count - 1];
            for (int i = 0; i < layer.Stops.Count - 1; i++)
            {
                StopSpec left = layer.Stops[i];
                StopSpec right = layer.Stops[i + 1];
                if (position >= left.Position && position <= right.Position)
                {
                    previous = left;
                    next = right;
                    break;
                }
            }

            if (position <= layer.Stops[0].Position)
            {
                previous = layer.Stops[0];
                next = previous;
            }
            else if (position >= layer.Stops[layer.Stops.Count - 1].Position)
            {
                previous = layer.Stops[layer.Stops.Count - 1];
                next = previous;
            }

            float u = Mathf.InverseLerp(previous.Position, next.Position, position);
            Color color = InterpolateGradientColors(previous, next, u);
            color.a = Mathf.Lerp(previous.Alpha, next.Alpha, u);
            return color;
        }

        static Color InterpolateGradientColors(StopSpec previous, StopSpec next, float t)
        {
            Color previousSrgb = previous.Color.gamma;
            Color nextSrgb = next.Color.gamma;

            if (previous.Alpha >= 0.999f && next.Alpha >= 0.999f)
            {
                const float gradientGamma = 1.1f;
                Color srgb = new Color(
                    Mathf.Pow(Mathf.Lerp(Mathf.Pow(Mathf.Max(previousSrgb.r, 1e-6f), gradientGamma), Mathf.Pow(Mathf.Max(nextSrgb.r, 1e-6f), gradientGamma), t), 1f / gradientGamma),
                    Mathf.Pow(Mathf.Lerp(Mathf.Pow(Mathf.Max(previousSrgb.g, 1e-6f), gradientGamma), Mathf.Pow(Mathf.Max(nextSrgb.g, 1e-6f), gradientGamma), t), 1f / gradientGamma),
                    Mathf.Pow(Mathf.Lerp(Mathf.Pow(Mathf.Max(previousSrgb.b, 1e-6f), gradientGamma), Mathf.Pow(Mathf.Max(nextSrgb.b, 1e-6f), gradientGamma), t), 1f / gradientGamma),
                    1f);
                return srgb.linear;
            }

            return Color.Lerp(previousSrgb, nextSrgb, t).linear;
        }

        void BlurTexture(RenderTexture source, RenderTexture temp, RenderTexture target, float blurRadius, bool premultiplyColor)
        {
            float blurStrength = ConvertFigmaBlurRadiusToSigma(blurRadius) * ResolveShadowSigmaScale();
            int blurPassCount = Mathf.Max(1, Mathf.CeilToInt((blurStrength * blurStrength) / (MaxShadowKernelExtent * MaxShadowKernelExtent)));
            float blurStrengthPerPass = blurStrength / Mathf.Sqrt(blurPassCount);

            RenderTexture inputRt = source;

            for (int passIndex = 0; passIndex < blurPassCount; passIndex++)
            {
                BuildGaussianBlurKernel(blurStrengthPerPass, out int tapCount, out List<float> weights, out List<float> offsets);

                Array.Clear(_shadowKernelWeightBuffer, 0, _shadowKernelWeightBuffer.Length);
                Array.Clear(_shadowKernelOffsetBuffer, 0, _shadowKernelOffsetBuffer.Length);
                for (int i = 0; i < tapCount && i < MaxShadowKernelTaps; i++)
                {
                    _shadowKernelWeightBuffer[i] = weights[i];
                    _shadowKernelOffsetBuffer[i] = offsets[i];
                }

                _blurMaterial.SetInt(PropTapCount, tapCount);
                _blurMaterial.SetFloatArray(PropWeights, _shadowKernelWeightBuffer);
                _blurMaterial.SetFloatArray(PropOffsets, _shadowKernelOffsetBuffer);
                _blurMaterial.SetTexture(PropSourceTex, inputRt);
                _blurMaterial.SetTexture(PropBlueNoise, shadowBlueNoiseTexture != null ? shadowBlueNoiseTexture : Texture2D.grayTexture);
                _blurMaterial.SetVector(PropBlueNoiseTexelSize, shadowBlueNoiseTexture != null
                    ? new Vector4(
                        1f / Mathf.Max(1, shadowBlueNoiseTexture.width),
                        1f / Mathf.Max(1, shadowBlueNoiseTexture.height),
                        shadowBlueNoiseTexture.width,
                        shadowBlueNoiseTexture.height)
                    : new Vector4(1f, 1f, 1f, 1f));
                _blurMaterial.SetVector(PropTargetSize, new Vector4(target.width, target.height, 0f, 0f));
                _blurMaterial.SetFloat(PropBlueNoiseStrength, 0f);
                _blurMaterial.SetFloat(PropProgressiveLayerBlur, 0f);
                _blurMaterial.SetFloat(PropBlurSrgb, premultiplyColor ? ResolveLayerBlurSrgb() : 0f);
                _blurMaterial.SetVector(PropSourceTexelSize, new Vector4(
                    1f / Mathf.Max(1, inputRt.width),
                    1f / Mathf.Max(1, inputRt.height),
                    inputRt.width,
                    inputRt.height));

                _blurMaterial.SetFloat(PropBlurAxis, 0f);
                _blurMaterial.SetFloat(PropPremultiplyInput, premultiplyColor && passIndex == 0 ? 1f : 0f);
                _blurMaterial.SetFloat(PropUnpremultiplyOutput, 0f);
                Graphics.Blit(Texture2D.blackTexture, temp, _blurMaterial);
                _blurMaterial.SetTexture(PropSourceTex, temp);
                _blurMaterial.SetVector(PropSourceTexelSize, new Vector4(
                    1f / Mathf.Max(1, temp.width),
                    1f / Mathf.Max(1, temp.height),
                    temp.width,
                    temp.height));
                _blurMaterial.SetFloat(PropBlurAxis, 1f);
                _blurMaterial.SetFloat(PropPremultiplyInput, 0f);
                _blurMaterial.SetFloat(PropUnpremultiplyOutput, premultiplyColor && passIndex == blurPassCount - 1 ? 1f : 0f);
                Graphics.Blit(Texture2D.blackTexture, target, _blurMaterial);

                inputRt = target;
            }
        }

        void BlurTextureProgressive(RenderTexture source, RenderTexture temp, RenderTexture target, ShaderRenderSpec spec)
        {
            float startSigma = ConvertFigmaBlurRadiusToSigma(Mathf.Min(spec.LayerBlur.StartRadius, spec.LayerBlur.Radius)) * ResolveShadowSigmaScale();
            float endSigma = ConvertFigmaBlurRadiusToSigma(spec.LayerBlur.Radius) * ResolveShadowSigmaScale();

            _blurMaterial.SetFloat(PropTapCount, ProgressiveBlurKernelTapCount);
            _blurMaterial.SetTexture(PropSourceTex, source);
            _blurMaterial.SetTexture(PropBlueNoise, shadowBlueNoiseTexture != null ? shadowBlueNoiseTexture : Texture2D.grayTexture);
            _blurMaterial.SetVector(PropBlueNoiseTexelSize, shadowBlueNoiseTexture != null
                ? new Vector4(
                    1f / Mathf.Max(1, shadowBlueNoiseTexture.width),
                    1f / Mathf.Max(1, shadowBlueNoiseTexture.height),
                    shadowBlueNoiseTexture.width,
                    shadowBlueNoiseTexture.height)
                : new Vector4(1f, 1f, 1f, 1f));
            _blurMaterial.SetVector(PropTargetSize, new Vector4(target.width, target.height, 0f, 0f));
            _blurMaterial.SetFloat(PropBlueNoiseStrength, 0f);
            _blurMaterial.SetFloat(PropProgressiveLayerBlur, 1f);
            _blurMaterial.SetFloat(PropBlurSrgb, ResolveLayerBlurSrgb());
            _blurMaterial.SetVector(PropCanvasSize, new Vector4(spec.CanvasSize.x, spec.CanvasSize.y, 0f, 0f));
            _blurMaterial.SetVector(PropShapeSize, new Vector4(spec.ShapeSize.x, spec.ShapeSize.y, 0f, 0f));
            _blurMaterial.SetVector(PropShapeOffset, new Vector4(spec.ShapeOffset.x, spec.ShapeOffset.y, 0f, 0f));
            _blurMaterial.SetFloat(PropShapeRotation, spec.ShapeRotationRadians);
            _blurMaterial.SetVector(PropLayerBlur, new Vector4(startSigma, endSigma, ResolveLayerBlurGamma(), 0f));
            _blurMaterial.SetVector(PropLayerBlurOffsets, new Vector4(
                spec.LayerBlur.StartOffset.x,
                spec.LayerBlur.StartOffset.y,
                spec.LayerBlur.EndOffset.x,
                spec.LayerBlur.EndOffset.y));
            _blurMaterial.SetVector(PropSourceTexelSize, new Vector4(
                1f / Mathf.Max(1, source.width),
                1f / Mathf.Max(1, source.height),
                source.width,
                source.height));
            _blurMaterial.SetFloat(PropBlurAxis, 0f);
            _blurMaterial.SetFloat(PropPremultiplyInput, 1f);
            _blurMaterial.SetFloat(PropUnpremultiplyOutput, 0f);
            Graphics.Blit(Texture2D.blackTexture, temp, _blurMaterial);

            _blurMaterial.SetTexture(PropSourceTex, temp);
            _blurMaterial.SetVector(PropSourceTexelSize, new Vector4(
                1f / Mathf.Max(1, temp.width),
                1f / Mathf.Max(1, temp.height),
                temp.width,
                temp.height));
            _blurMaterial.SetFloat(PropBlurAxis, 1f);
            _blurMaterial.SetFloat(PropPremultiplyInput, 0f);
            _blurMaterial.SetFloat(PropUnpremultiplyOutput, 1f);
            Graphics.Blit(Texture2D.blackTexture, target, _blurMaterial);
            _blurMaterial.SetFloat(PropProgressiveLayerBlur, 0f);
        }

        static float ConvertFigmaBlurRadiusToSigma(float blurRadius)
        {
            return Mathf.Max(0f, blurRadius);
        }

        float ResolveDiamondExponent()
        {
            return 1f;
        }

        float ResolveStrokeAngularOffset()
        {
            return 0f;
        }

        float ResolveShadowSigmaScale()
        {
            if (_shadowSigmaScaleCache.HasValue)
            {
                return _shadowSigmaScaleCache.Value;
            }

            _shadowSigmaScaleCache = TryResolveEnvScale(ShadowSigmaScaleEnv, DefaultShadowSigmaScale);
            return _shadowSigmaScaleCache.Value;
        }

        float ResolveShadowSpreadScale()
        {
            if (_shadowSpreadScaleCache.HasValue)
            {
                return _shadowSpreadScaleCache.Value;
            }

            _shadowSpreadScaleCache = TryResolveEnvScale(ShadowSpreadScaleEnv, 1f);
            return _shadowSpreadScaleCache.Value;
        }

        float ResolveShadowAlphaScale()
        {
            if (_shadowAlphaScaleCache.HasValue)
            {
                return _shadowAlphaScaleCache.Value;
            }

            _shadowAlphaScaleCache = TryResolveEnvScale(ShadowAlphaScaleEnv, 1f);
            return _shadowAlphaScaleCache.Value;
        }

        float ResolveShadowOffsetXScale()
        {
            if (_shadowOffsetXScaleCache.HasValue)
            {
                return _shadowOffsetXScaleCache.Value;
            }

            _shadowOffsetXScaleCache = TryResolveEnvScale(ShadowOffsetXScaleEnv, 1f);
            return _shadowOffsetXScaleCache.Value;
        }

        float ResolveShadowOffsetYScale()
        {
            if (_shadowOffsetYScaleCache.HasValue)
            {
                return _shadowOffsetYScaleCache.Value;
            }

            _shadowOffsetYScaleCache = TryResolveEnvScale(ShadowOffsetYScaleEnv, 1f);
            return _shadowOffsetYScaleCache.Value;
        }

        float ResolveShadowNearAlphaScale()
        {
            if (_shadowNearAlphaScaleCache.HasValue)
            {
                return _shadowNearAlphaScaleCache.Value;
            }

            _shadowNearAlphaScaleCache = TryResolveEnvScale(ShadowNearAlphaScaleEnv, 1f);
            return _shadowNearAlphaScaleCache.Value;
        }

        float ResolveShadowFarAlphaScale()
        {
            if (_shadowFarAlphaScaleCache.HasValue)
            {
                return _shadowFarAlphaScaleCache.Value;
            }

            _shadowFarAlphaScaleCache = TryResolveEnvScale(ShadowFarAlphaScaleEnv, 1f);
            return _shadowFarAlphaScaleCache.Value;
        }

        float ResolveLayerBlurGamma()
        {
            return Mathf.Max(LayerBlurMinimumScale, TryResolveEnvScale(LayerBlurGammaEnv, LayerBlurGammaDefault));
        }

        static float ResolveLayerBlurRadiusScale()
        {
            return Mathf.Max(LayerBlurMinimumScale, TryResolveEnvScale(LayerBlurRadiusScaleEnv, LayerBlurRadiusScaleDefault));
        }

        static float ResolveLayerBlurStartRadiusScale()
        {
            return Mathf.Max(0f, TryResolveEnvScale(LayerBlurStartRadiusScaleEnv, LayerBlurStartRadiusScaleDefault));
        }

        static float ResolveLayerBlurRampScale(ShaderRenderSpec spec)
        {
            float fallback = spec != null && spec.LayerBlurTuningMode == LayerBlurTuningModeOverlay
                ? LayerBlurRampScaleTuningModeOverlay
                : spec != null && spec.LayerBlurTuningMode == LayerBlurTuningModeShadow
                    ? LayerBlurRampScaleTuningModeShadow
                    : LayerBlurRampScaleDefault;
            return Mathf.Max(LayerBlurMinimumScale, TryResolveEnvScale(LayerBlurRampScaleEnv, fallback));
        }

        static float ResolveLayerBlurBias(ShaderRenderSpec spec)
        {
            return TryResolveEnvScale(LayerBlurBiasEnv, spec != null && spec.LayerBlurTuningMode == LayerBlurTuningModeShadow ? LayerBlurBiasTuningModeShadow : LayerBlurBiasDefault);
        }

        static float ResolveLayerBlurUvBlend(ShaderRenderSpec spec)
        {
            return TryResolveEnvScale(LayerBlurUvBlendEnv, spec != null && spec.LayerBlurTuningMode == LayerBlurTuningModeShadow ? LayerBlurUvBlendTuningModeShadow : 0f);
        }

        static float ResolveLayerBlurRadiusCurve(ShaderRenderSpec spec)
        {
            float fallback = spec != null && spec.LayerBlurTuningMode == LayerBlurTuningModeOverlay
                ? LayerBlurRadiusCurveTuningModeOverlay
                : spec != null && spec.LayerBlurTuningMode == LayerBlurTuningModeShadow
                    ? LayerBlurRadiusCurveTuningModeShadow
                    : 1f;
            return Mathf.Max(LayerBlurMinimumScale, TryResolveEnvScale(LayerBlurRadiusCurveEnv, fallback));
        }

        static float ResolveLayerBlurSrgb()
        {
            return TryResolveEnvScale(LayerBlurSrgbEnv, 0f) > LayerBlurSrgbThreshold ? 1f : 0f;
        }

        static bool ResolveLayerBlurVariable()
        {
            string raw = Environment.GetEnvironmentVariable(LayerBlurVariableEnv);
            return string.Equals(raw, EnvEnabledNumericValue, StringComparison.OrdinalIgnoreCase)
                || string.Equals(raw, EnvEnabledTextValue, StringComparison.OrdinalIgnoreCase);
        }

        static float InterpolateProgressiveRadius(float startRadius, float endRadius, float t, float curve)
        {
            float clampedT = Mathf.Clamp01(t);
            if (Mathf.Approximately(startRadius, endRadius))
            {
                return startRadius;
            }

            return Mathf.Lerp(startRadius, endRadius, Mathf.Pow(clampedT, curve));
        }

        static float TryResolveEnvScale(string envName, float fallback)
        {
            string raw = Environment.GetEnvironmentVariable(envName);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            if (!float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return fallback;
            }

            if (!float.IsFinite(value))
            {
                return fallback;
            }

            return value;
        }

        static float ComputeShadowPadding(float blurStrength)
        {
            if (blurStrength <= 0f)
            {
                return 0f;
            }

            return Mathf.Ceil(blurStrength * 3f);
        }

        static void BuildGaussianBlurKernel(float blurStrength, out int tapCount, out List<float> weights, out List<float> offsets)
        {
            if (blurStrength <= 0f)
            {
                tapCount = 1;
                weights = new List<float> { 1f };
                offsets = new List<float> { 0f };
                return;
            }

            int extent = Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(blurStrength, MaxShadowKernelExtent)), 1, MaxShadowKernelExtent);
            float sigma = Mathf.Max(blurStrength * 0.5f, GaussianSigmaFloor);
            float scaleFactor = GaussianSqrtHalf / sigma;
            float[] rawWeights = new float[extent + 1];

            float sumWeight = rawWeights[0] = 1f;
            for (int i = 1; i <= extent; i++)
            {
                float weight;
                if (sigma < GaussianAnalyticalThreshold)
                {
                    float p1 = ApproximateErrorFunction((i + 0.5f) * scaleFactor);
                    float p2 = ApproximateErrorFunction((i - 0.5f) * scaleFactor);
                    weight = (p1 - p2) * 0.5f;
                }
                else
                {
                    weight = Mathf.Exp(-(i * i) / (sigma * sigma));
                }

                rawWeights[i] = weight;
                sumWeight += weight * 2f;
            }

            float normalization = 1f / sumWeight;
            for (int i = 0; i <= extent; i++)
            {
                rawWeights[i] *= normalization;
            }

            tapCount = Mathf.Min(Mathf.CeilToInt(extent / 2f) + 1, MaxShadowKernelTaps);
            weights = new List<float>(new float[tapCount]);
            offsets = new List<float>(new float[tapCount]);
            weights[0] = rawWeights[0];

            for (int i = 1; i < tapCount; i++)
            {
                int right = i * 2;
                int left = right - 1;
                float leftWeight = rawWeights[left];
                float rightWeight = right <= extent ? rawWeights[right] : 0f;
                float packedWeight = leftWeight + rightWeight;
                weights[i] = packedWeight;
                offsets[i] = packedWeight > GaussianPackedWeightEpsilon ? left + rightWeight / packedWeight : left;
            }
        }

        static float ApproximateErrorFunction(float x)
        {
            int sign = x < 0f ? -1 : 1;
            x = Mathf.Abs(x);
            float t = 1f / (1f + ErfP * x);
            float y = 1f - (((((ErfA5 * t + ErfA4) * t) + ErfA3) * t + ErfA2) * t + ErfA1) * t * Mathf.Exp(-x * x);
            return sign * y;
        }

        List<ShadowRenderPlan> BuildShadowPlans(ShaderRenderSpec spec, ExportEntry entry, float shadowStrokeExpand)
        {
            List<ShadowRenderPlan> plans = new List<ShadowRenderPlan>(spec.DropShadows.Count);

            for (int i = 0; i < spec.DropShadows.Count; i++)
            {
                ShadowSpec shadow = spec.DropShadows[i];
                float spreadPx = shadow.Spread * ResolveShadowSpreadScale() * AdaptiveShadowSpreadScale;
                if (spreadPx < 0f)
                {
                    float negativeSpreadScale = Mathf.Lerp(NegativeSpreadScaleNear, NegativeSpreadScaleFar, Mathf.InverseLerp(NegativeSpreadBlurNear, NegativeSpreadBlurFar, shadow.BlurRadius));
                    spreadPx *= negativeSpreadScale;
                }

                Vector2 scaledShadowOffset = new Vector2(
                    shadow.Offset.x * ResolveShadowOffsetXScale(),
                    shadow.Offset.y * ResolveShadowOffsetYScale());
                float blurStrength = ConvertFigmaBlurRadiusToSigma(shadow.BlurRadius);
                float blurPadding = ComputeShadowPadding(blurStrength);
                float maskSpreadPx = spreadPx;
                float positiveSpreadPx = Mathf.Max(0f, spreadPx);
                float padX = Mathf.Ceil(blurPadding + positiveSpreadPx + Mathf.Abs(scaledShadowOffset.x) + shadowStrokeExpand + 2f);
                float padY = Mathf.Ceil(blurPadding + positiveSpreadPx + Mathf.Abs(scaledShadowOffset.y) + shadowStrokeExpand + 2f);
                Vector2 shadowCanvasSize = new Vector2(
                    Mathf.Max(1f, spec.CanvasSize.x + padX * 2f),
                    Mathf.Max(1f, spec.CanvasSize.y + padY * 2f));

                plans.Add(new ShadowRenderPlan
                {
                    Shadow = shadow,
                    BlurRadius = shadow.BlurRadius * AdaptiveShadowSigmaScale,
                    SpreadPx = spreadPx,
                    MaskSpreadPx = maskSpreadPx,
                    PostBlurAlphaMultiplier = 1f,
                    ScaledShadowOffset = scaledShadowOffset,
                    ShadowCanvasSize = shadowCanvasSize,
                    ShadowRect = new Rect(-padX, -padY, shadowCanvasSize.x, shadowCanvasSize.y)
                });
            }

            return plans;
        }

        static RenderTexture CreateRenderTexture(int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            return rt;
        }

        static void ClearRenderTexture(RenderTexture rt)
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.clear);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        static void ReleaseTemporary(RenderTexture rt)
        {
            if (rt != null)
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        static byte[] EncodeRenderTexturePng(RenderTexture rt)
        {
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true);

            try
            {
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
                texture.Apply(false, false);
                return texture.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previous;
                DestroyUnityObject(texture);
            }
        }

        static Rect ResolveContainerCanvasBounds(ExportEntry entry)
        {
            if (entry.UseExternalCanvasBounds && TryGetRect(entry.CanvasBounds, out Rect externalBounds))
                return externalBounds;

            if (TryGetRect(entry.Node.AbsoluteRenderBounds, out Rect renderBounds))
                return renderBounds;

            if (TryGetRect(entry.Node.AbsoluteBoundingBox, out Rect contentBounds))
                return contentBounds;

            throw new InvalidOperationException(MissingAbsoluteBoundingBoxError);
        }

        bool HasRenderableChildren(FigmageNode node)
        {
            if (node?.Children == null)
                return false;

            for (int i = 0; i < node.Children.Count; i++)
            {
                if (HasRenderableNode(node.Children[i]))
                    return true;
            }

            return false;
        }

        bool HasRenderableNode(FigmageNode node)
        {
            if (node == null)
                return false;

            if (TryBuildRenderSpec(node, 1f, out _, out _))
                return true;

            return HasRenderableChildren(node);
        }

        static Texture2D DecodePng(byte[] bytes)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!texture.LoadImage(bytes))
            {
                DestroyUnityObject(texture);
                throw new InvalidOperationException("Failed to decode rendered child PNG.");
            }

            return texture;
        }

        static void CompositeOver(Color32[] destination, Color32[] source)
        {
            for (int i = 0; i < destination.Length; i++)
            {
                Color32 src = source[i];
                if (src.a == 0)
                    continue;

                if (src.a == 255)
                {
                    destination[i] = src;
                    continue;
                }

                Color32 dst = destination[i];
                float srcA = src.a / 255f;
                float dstA = dst.a / 255f;
                float outA = srcA + dstA * (1f - srcA);
                if (outA <= 0f)
                {
                    destination[i] = default;
                    continue;
                }

                float srcWeight = srcA / outA;
                float dstWeight = dstA * (1f - srcA) / outA;
                destination[i] = new Color32(
                    ToByte(src.r * srcWeight + dst.r * dstWeight),
                    ToByte(src.g * srcWeight + dst.g * dstWeight),
                    ToByte(src.b * srcWeight + dst.b * dstWeight),
                    ToByte(outA * 255f));
            }
        }

        static byte ToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255);
        }

        static void DestroyUnityObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
