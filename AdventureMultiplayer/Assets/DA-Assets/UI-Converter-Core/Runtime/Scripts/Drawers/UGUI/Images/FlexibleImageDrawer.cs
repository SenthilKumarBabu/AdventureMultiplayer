#if UNITY_EDITOR
#if FLEXIBLE_IMAGE_EXISTS
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using JeffGrawAssets.FlexibleUI;
using System;
using System.Linq;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class FlexibleImageDrawer : FcuBase
    {
        public void Draw(Node fobject, Sprite sprite, GameObject target)
        {
            target.TryAddGraphic(out FlexibleImage img);

            ApplyImageSettings(img);
            SetCorners(fobject, img);

            img.sprite = sprite;

            if (fobject.IsDrawableType())
            {
                img.UseAdvancedRaycast = true;
                SetProceduralVisuals(fobject, img);
            }
            else
            {
                img.UseAdvancedRaycast = false;
                monoBeh.CanvasDrawer.ImageDrawer.UnityImageDrawer.SetColor(fobject, img);
            }
        }

        private void ApplyImageSettings(FlexibleImage img)
        {
            var settings = monoBeh.Settings.FlexibleImageSettings;

            img.type = settings.Type;
            img.raycastTarget = settings.RaycastTarget;
            img.preserveAspect = settings.PreserveAspect;
            img.maskable = settings.Maskable;
            img.NormalRaycastPadding = settings.RaycastPadding;
            img.DataMode = FlexibleImage.QuadDataMode.Single;
            img.color = Color.white;
#if UNITY_2020_1_OR_NEWER
            img.raycastPadding = settings.RaycastPadding;
#endif
        }

        private void SetProceduralVisuals(Node fobject, FlexibleImage img)
        {
            var quadData = img.PrimaryQuadData;
            var props = quadData.DefaultProceduralProps;
            var graphic = fobject.Data.Graphic;
            var settings = monoBeh.Settings.FlexibleImageSettings;

            quadData.Enabled = true;
            props.SetDefaultColors();

            quadData.PrimaryColorDimensions = Vector2Int.one;
            quadData.OutlineColorDimensions = Vector2Int.one;
            quadData.ProceduralGradientColorDimensions = Vector2Int.one;
            quadData.PatternColorDimensions = Vector2Int.one;
            quadData.PatternAffectsInterior = false;
            quadData.PatternAffectsOutline = false;
            quadData.OutlineAdjustsChamfer = false;
            quadData.AddInteriorOutline = false;
            quadData.OutlineExpandsOutward = false;
            quadData.SoftnessFeatherMode = ToQuadFeatherMode(settings.FeatherMode);
            props.softness = settings.Softness;
            img.MeshSubdivisions = settings.MeshSubdivisions;

            SetFill(fobject, quadData, props);
            SetStroke(fobject, quadData, props);

            monoBeh.CanvasDrawer.ImageDrawer.RemoveDAOutline(fobject.Data.GameObject);
        }

        private void SetFill(Node fobject, QuadData quadData, ProceduralProperties props)
        {
            FGraphic graphic = fobject.Data.Graphic;

            if (!graphic.HasFill)
            {
                props.SetPrimaryColor(Color.clear);
                quadData.PrimaryColorFade = 0;
                fobject.SetReason(ReasonKey.Fill_Transparent);
                return;
            }

            if (graphic.Fill.HasSolid)
            {
                Color fillColor = graphic.Fill.SolidPaint.Color;
                props.SetPrimaryColor(fillColor);
                quadData.PrimaryColorFade = (byte)Mathf.Clamp(Mathf.RoundToInt(fillColor.a * 255f), 0, 255);
                fobject.SetReason(ReasonKey.Fill_SolidColor);
            }
            else if (graphic.Fill.HasGradient)
            {
                ApplyGradientFill(fobject, graphic.Fill.GradientPaint, quadData, props);
                fobject.SetReason(ReasonKey.Fill_GradientNative);
            }
        }

        private void ApplyGradientFill(Node fobject, Paint gradient, QuadData quadData, ProceduralProperties props)
        {
            quadData.ProceduralGradientAffectsInterior = true;
            quadData.ProceduralGradientAffectsOutline = false;
            quadData.ProceduralGradientAspectCorrection = true;
            quadData.PrimaryColorFade = 255;

            props.SetPrimaryColor(Color.white);

            if (gradient.GradientStops.IsEmpty())
            {
                props.SetProceduralGradientColor(Color.white);
                quadData.ProceduralGradientColorDimensions = Vector2Int.one;
                return;
            }

            var first = gradient.GradientStops.First();
            var last = gradient.GradientStops.Last();

            props.SetProceduralGradientColor(first.Color);
            props.SetProceduralGradientColorAtCell(1, 0, last.Color);
            quadData.ProceduralGradientColorDimensions = new Vector2Int(2, 1);

            switch (gradient.Type)
            {
                case PaintType.GRADIENT_RADIAL:
                    quadData.ProceduralGradientType = QuadData.GradientType.Radial;
                    quadData.RadialGradientStrength = 1f;
                    quadData.RadialGradientSize = Vector2.zero;
                    quadData.ProceduralGradientPosition = gradient.GradientHandlePositions.IsEmpty()
                        ? Vector2.zero
                        : gradient.GradientHandlePositions[0];
                    break;
                default:
                    quadData.ProceduralGradientType = QuadData.GradientType.Angle;
                    quadData.AngleGradientStrength = Vector2.one;
                    quadData.ProceduralGradientColorRotation = monoBeh.GraphicHelpers.ToLinearAngle(fobject, gradient.GradientHandlePositions);
                    break;
            }
        }

        private void SetStroke(Node fobject, QuadData quadData, ProceduralProperties props)
        {
            FGraphic graphic = fobject.Data.Graphic;

            if (!graphic.HasStroke)
            {
                quadData.SetOutlineWidth(0f);
                fobject.SetReason(ReasonKey.Stroke_None);
                return;
            }

            quadData.SetOutlineWidth(fobject.StrokeWeight);
            quadData.OutlineAdjustsChamfer = true;

            if (graphic.Stroke.HasGradient)
            {
                var stops = graphic.Stroke.GradientPaint.ToGradientColorKeys();
                if (stops.IsEmpty())
                {
                    props.SetOutlineColor(graphic.Stroke.SingleColor);
                    quadData.OutlineColorDimensions = Vector2Int.one;
                }
                else
                {
                    Color first = stops.FirstOrDefault().color;
                    Color last = stops.LastOrDefault().color;

                    props.SetOutlineColor(first);
                    if (stops.Count > 1)
                    {
                        props.SetOutlineColorAtCell(1, 0, last);
                        quadData.OutlineColorDimensions = new Vector2Int(2, 1);
                    }
                    else
                    {
                        quadData.OutlineColorDimensions = Vector2Int.one;
                    }
                }
            }
            else
            {
                Color strokeColor = graphic.Stroke.HasSolid ? graphic.Stroke.SolidPaint.Color : graphic.Stroke.SingleColor;
                props.SetOutlineColor(strokeColor);
                quadData.OutlineColorDimensions = Vector2Int.one;
            }

            StrokeAlign align = graphic.Stroke.Align;
            quadData.OutlineExpandsOutward = align == StrokeAlign.OUTSIDE;
            quadData.AddInteriorOutline = align == StrokeAlign.INSIDE;

            fobject.SetReason(ReasonKey.Stroke_NativeOutline);
        }

        private void SetCorners(Node fobject, FlexibleImage img)
        {
            QuadData quadData = img.PrimaryQuadData;
            quadData.NormalizeChamfer = true;
            quadData.CornerChamfer = monoBeh.GraphicHelpers.GetCornerRadius(fobject);
            quadData.CornerConcavity = Vector4.zero;
        }

        private QuadData.FeatherMode ToQuadFeatherMode(FlexibleImageSettings.FlexibleImageFeatherMode mode)
        {
            return mode switch
            {
                FlexibleImageSettings.FlexibleImageFeatherMode.Inwards => QuadData.FeatherMode.Inwards,
                FlexibleImageSettings.FlexibleImageFeatherMode.Outwards => QuadData.FeatherMode.Outwards,
                _ => QuadData.FeatherMode.Bidirectional
            };
        }
    }
}
#endif
#endif