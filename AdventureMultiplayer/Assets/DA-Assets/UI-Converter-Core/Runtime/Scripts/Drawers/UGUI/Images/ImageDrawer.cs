#if UNITY_EDITOR
using DA_Assets.CR;
using DA_Assets.DAO;
using DA_Assets.DAG;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
#if DA_UI_COMPONENTS_EXISTS
using DA_Assets.UI;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class ImageDrawer : FcuBase
    {
        public override void Init(ConverterBase monoBeh)
        {
            base.Init(monoBeh);

            this.UnityImageDrawer.Init(monoBeh);
            this.SpriteRendererDrawer.Init(monoBeh);

#if SUBC_SHAPES_EXISTS
            this.Shapes2DDrawer.Init(monoBeh);
#endif

#if JOSH_PUI_EXISTS
            this.JoshPuiDrawer.Init(monoBeh);
#endif

#if PROCEDURAL_UI_ASSET_STORE_RELEASE
            this.DttPuiDrawer.Init(monoBeh);
#endif

#if MPUIKIT_EXISTS
            this.MPUIKitDrawer.Init(monoBeh);
#endif

#if VECTOR_GRAPHICS_EXISTS
            this.SvgImageDrawer.Init(monoBeh);
#endif

#if NOVA_UI_EXISTS
            this.NovaImageDrawer.Init(monoBeh);
#endif

#if FLEXIBLE_IMAGE_EXISTS
            this.FlexibleImageDrawer.Init(monoBeh);
#endif
        }

        public void Draw(Node fobject, GameObject customGameObject = null)
        {
            GameObject target = customGameObject == null ? fobject.Data.GameObject : customGameObject;

            if (fobject.Data.GameObject.IsPartOfAnyPrefab() == false)
            {
                if (target.TryGetComponentSafe(out Graphic oldGraphic))
                {
                    Type curType = monoBeh.GetCurrentImageType();

                    if (oldGraphic.GetType().Equals(curType) == false)
                    {
                        oldGraphic.RemoveComponentsDependingOn();
                        oldGraphic.Destroy();
                    }
                }
            }

            Sprite sprite = monoBeh.SpriteProcessor.GetSprite(fobject);

            if (sprite == null)
            {
                if (fobject.IsSingleImageOrVideoOrEmojiType() || fobject.IsSprite())
                {
                    sprite = monoBeh.Config.MissingImageTexture128px;
                }
            }

            if (monoBeh.IsNova())
            {
#if NOVA_UI_EXISTS
                this.NovaImageDrawer.Draw(fobject, sprite, target);
#endif
            }
            else if (monoBeh.UsingUnityImage() || monoBeh.UsingRawImage() || fobject.IsObjectMask() || fobject.CanUseUnityImage(monoBeh))
            {
                this.UnityImageDrawer.Draw(fobject, sprite, target);
            }
            else if (monoBeh.UsingSvgImage())
            {
#if VECTOR_GRAPHICS_EXISTS
                this.SvgImageDrawer.Draw(fobject, sprite, target);
#endif
            }
            else if (monoBeh.UsingSpriteRenderer())
            {
                this.SpriteRendererDrawer.Draw(fobject, sprite, target);
            }
            else if (monoBeh.UsingShapes2D())
            {
#if SUBC_SHAPES_EXISTS
                this.Shapes2DDrawer.Draw(fobject, sprite, target);
#endif
            }
            else if (monoBeh.UsingJoshPui())
            {
#if JOSH_PUI_EXISTS
                this.JoshPuiDrawer.Draw(fobject, sprite, target);
#endif
            }
            else if (monoBeh.UsingDttPui())
            {
#if PROCEDURAL_UI_ASSET_STORE_RELEASE
                this.DttPuiDrawer.Draw(fobject, sprite, target);
#endif
            }
            else if (monoBeh.UsingMPUIKit())
            {
#if MPUIKIT_EXISTS
                this.MPUIKitDrawer.Draw(fobject, sprite, target);
#endif
            }
            else if (monoBeh.UsingFlexibleImage())
            {
#if FLEXIBLE_IMAGE_EXISTS
                this.FlexibleImageDrawer.Draw(fobject, sprite, target);
#endif
            }

            if (fobject.IsSprite() && (fobject.ContainsTag(FcuTag.Slice9) || fobject.ContainsTag(FcuTag.AutoSlice9)))
            {
                if (!monoBeh.IsNova())
                {
                    if (target.TryGetComponent(out Image img))
                    {
                        img.type = Image.Type.Sliced;
                        ApplyAutoSlice9PixelsPerUnit(fobject, img);
                    }
                }
            }
        }

        private void ApplyAutoSlice9PixelsPerUnit(Node fobject, Image img)
        {
            if (!fobject.ContainsTag(FcuTag.AutoSlice9) || img.sprite == null)
                return;

            if (!fobject.TryGetAutoSlice9SourceBorder(out Vector4 targetBorder))
                return;

            Vector4 spriteBorder = img.sprite.border;

            if (!TryGetPixelsPerUnitMultiplier(img, targetBorder, spriteBorder, out float multiplier))
                return;

            img.pixelsPerUnitMultiplier = multiplier;
        }

        private static bool TryGetPixelsPerUnitMultiplier(Image img, Vector4 targetBorder, Vector4 spriteBorder, out float multiplier)
        {
            multiplier = 0f;

            if (img.canvas == null || img.canvas.referencePixelsPerUnit <= 0f || img.sprite.pixelsPerUnit <= 0f)
                return false;

            float basePixelsPerUnit = img.sprite.pixelsPerUnit / img.canvas.referencePixelsPerUnit;
            float sum = 0f;
            int count = 0;

            AddBorderMultiplier(spriteBorder.x, targetBorder.x, basePixelsPerUnit, ref sum, ref count);
            AddBorderMultiplier(spriteBorder.y, targetBorder.y, basePixelsPerUnit, ref sum, ref count);
            AddBorderMultiplier(spriteBorder.z, targetBorder.z, basePixelsPerUnit, ref sum, ref count);
            AddBorderMultiplier(spriteBorder.w, targetBorder.w, basePixelsPerUnit, ref sum, ref count);

            if (count == 0)
                return false;

            multiplier = sum / count;
            return multiplier > 0f;
        }

        private static void AddBorderMultiplier(float spriteBorder, float targetBorder, float basePixelsPerUnit, ref float sum, ref int count)
        {
            if (spriteBorder <= 0f || targetBorder <= 0f)
                return;

            sum += spriteBorder / (targetBorder * basePixelsPerUnit);
            count++;
        }

        public void AddDAOutline(Node fobject)
        {
            GameObject gameObject = fobject.Data.GameObject;
            gameObject.TryDestroyComponent<UnityEngine.UI.Outline>();

            if (!fobject.TryToDAOutlineData(out DAOutlineData outlineData))
            {
                RemoveDAOutline(gameObject);
                fobject.SetReason(ReasonKey.Stroke_Ignored);
                return;
            }

            gameObject.TryAddComponent(out DAOutlineEffect outline);
            outline.SetData(outlineData);
            fobject.SetReason(ReasonKey.Stroke_DAOutline);
        }

        public void RemoveDAOutline(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            gameObject.TryDestroyComponent<UnityEngine.UI.Outline>();
            gameObject.TryDestroyComponent<DAOutlineEffect>();
        }

        public void ClearBakedSpriteOverlays(Node fobject)
        {
            GameObject gameObject = fobject.Data.GameObject;
            if (gameObject == null)
                return;

            RemoveDAOutline(gameObject);
#if DA_UI_COMPONENTS_EXISTS
            gameObject.TryDestroyComponent<SpriteOutline>();
#endif
            gameObject.TryDestroyComponent<DAGradient>();
            gameObject.TryDestroyComponent<CornerRounder>();
        }

        public void AddGradient(Node fobject, Paint gradientColor, bool strokeOnly = false)
        {
            GameObject gameObject = fobject.Data.GameObject;
            List<GradientColorKey> gradientColorKeys = gradientColor.ToGradientColorKeys();
            List<GradientAlphaKey> gradientAlphaKeys = gradientColor.ToGradientAlphaKeys();

            float angle;

            switch (gradientColor.Type)
            {
                case PaintType.GRADIENT_RADIAL:
                    angle = monoBeh.GraphicHelpers.ToRadialAngle(fobject, gradientColor.GradientHandlePositions);
                    break;
                default:
                    angle = monoBeh.GraphicHelpers.ToLinearAngle(fobject, gradientColor.GradientHandlePositions);
                    break;
            }

            if (monoBeh.UsingShapes2D() && !strokeOnly)
            {
                AddDAGradient();
            }
            else if (monoBeh.UsingMPUIKit())
            {
#if MPUIKIT_EXISTS
                if (gameObject.TryGetComponentSafe(out MPUIKIT.MPImage img))
                {
                    Gradient gradient = new Gradient
                    {
                        mode = GradientMode.Blend,
                    };

                    MPUIKIT.GradientEffect ge = new MPUIKIT.GradientEffect();
                    ge.Enabled = true;

                    switch (gradientColor.Type)
                    {
                        case PaintType.GRADIENT_RADIAL:
                            ge.GradientType = MPUIKIT.GradientType.Radial;
                            break;
                        default:
                            ge.GradientType = MPUIKIT.GradientType.Linear;
                            break;
                    }

                    ge.Gradient = gradient;
                    ge.Rotation = angle;
                    img.GradientEffect = ge;

                    gradient.colorKeys = gradientColorKeys.ToArray();
                    gradient.alphaKeys = gradientAlphaKeys.ToArray();
                }
                else
                {
                    AddDAGradient();
                }
#endif
            }
            else if (monoBeh.UsingDttPui())
            {
#if PROCEDURAL_UI_ASSET_STORE_RELEASE
                if (gameObject.TryGetComponentSafe(out DTT.UI.ProceduralUI.RoundedImage roundedImage))
                {
                    gameObject.TryAddComponent(out DTT.UI.ProceduralUI.GradientEffect gradient);

                    Gradient newGradient = new Gradient();
                    newGradient.colorKeys = gradientColorKeys.ToArray();
                    newGradient.alphaKeys = gradientAlphaKeys.ToArray();

                    Type objectType = gradient.GetType();
                    System.Reflection.FieldInfo fieldInfo = objectType.GetField("_gradient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(gradient, newGradient);
                    }

                    System.Reflection.FieldInfo fieldInfo1 = objectType.GetField("_type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (fieldInfo1 != null)
                    {
                        DTT.UI.ProceduralUI.GradientEffect.GradientType gt;

                        switch (gradientColor.Type)
                        {
                            case PaintType.GRADIENT_RADIAL:
                                gt = DTT.UI.ProceduralUI.GradientEffect.GradientType.ANGULAR;
                                break;
                            case PaintType.GRADIENT_ANGULAR:
                                gt = DTT.UI.ProceduralUI.GradientEffect.GradientType.RADIAL;
                                break;
                            default:
                                gt = DTT.UI.ProceduralUI.GradientEffect.GradientType.LINEAR;
                                break;
                        }

                        fieldInfo1.SetValue(gradient, gt);
                    }

                    System.Reflection.FieldInfo fieldInfo3 = objectType.GetField("_rotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (fieldInfo3 != null)
                    {
                        fieldInfo3.SetValue(gradient, angle);
                    }
                }
                else
                {
                    AddDAGradient();
                }
#endif
            }
            else if (monoBeh.UsingFlexibleImage())
            {

            }
            else
            {
                AddDAGradient();
            }

            void AddDAGradient()
            {
                gameObject.TryAddComponent(out DAGradient gradient);

                gradient.Angle = angle;
                gradient.BlendMode = DAColorBlendMode.Multiply;

                gradient.Gradient.colorKeys = gradientColorKeys.ToArray();
                gradient.Gradient.alphaKeys = gradientAlphaKeys.ToArray();
            }
        }

        public bool TryAddCornerRounder(Node fobject, GameObject target)
        {
            if (fobject.IsGenerativeType())
            {
                target.TryDestroyComponent<CornerRounder>();
                return false;
            }

            if (fobject.IsSprite())
            {
                return false;
            }

            if (fobject.ContainsRoundedCorners())
            {
                target.TryAddComponent(out CornerRounder cornerRounder);
                Vector4 cr = monoBeh.GraphicHelpers.GetCornerRadius(fobject);
                cornerRounder.SetRadii(cr);
                RecreateDAOutlineAfterCornerRounder(target);
            }

            return false;
        }

        private void RecreateDAOutlineAfterCornerRounder(GameObject target)
        {
            if (!target.TryGetComponent(out DAOutlineEffect outline))
                return;

            DAOutlineData data = outline.GetData();
            outline.Destroy();
            target.TryAddComponent(out DAOutlineEffect recreatedOutline);
            recreatedOutline.SetData(data);
        }

        public void SetProceduralColor(Node fobject, Image img, Action setStrokeOnlyWidth, Action setStroke)
        {
            FGraphic graphic = fobject.Data.Graphic;

            FcuLogger.Debug($"SetUnityImageColor | {fobject.Data.NameHierarchy} | {fobject.Data.FcuImageType} | hasFills: {graphic.HasFill} | hasStroke: {graphic.HasStroke}", FcuDebugSettingsFlags.LogComponentDrawer);

            bool strokeOnly = graphic.HasStroke && !graphic.HasFill;

            if (strokeOnly)
            {
                setStrokeOnlyWidth();

                fobject.SetReason(ReasonKey.Fill_Transparent);

                if (graphic.Stroke.HasSolid)
                {
                    img.color = graphic.Stroke.SolidPaint.Color;
                }
                else if (graphic.Stroke.HasGradient)
                {
                    img.color = Color.white;
                    monoBeh.CanvasDrawer.ImageDrawer.AddGradient(fobject, graphic.Stroke.GradientPaint);
                }
            }
            else
            {
                if (graphic.Fill.HasSolid)
                {
                    img.color = graphic.Fill.SolidPaint.Color;
                    fobject.SetReason(ReasonKey.Fill_SolidColor);
                }
                else if (graphic.Fill.HasGradient)
                {
                    img.color = Color.white;
                    monoBeh.CanvasDrawer.ImageDrawer.AddGradient(fobject, graphic.Fill.GradientPaint);
                    fobject.SetReason(ReasonKey.Fill_GradientComponent);
                }

                if (graphic.HasStroke)
                {
                    setStroke();
                }
            }

            if (!graphic.HasStroke)
            {
                RemoveDAOutline(fobject.Data.GameObject);
            }
        }

        [SerializeField] public UnityImageDrawer UnityImageDrawer = new UnityImageDrawer();

        [SerializeField] public SpriteRendererDrawer SpriteRendererDrawer = new SpriteRendererDrawer();

#if SUBC_SHAPES_EXISTS
        [SerializeField] public Shapes2DDrawer Shapes2DDrawer = new Shapes2DDrawer();
#endif

#if JOSH_PUI_EXISTS
        [SerializeField] public JoshPuiDrawer JoshPuiDrawer = new JoshPuiDrawer();
#endif

#if PROCEDURAL_UI_ASSET_STORE_RELEASE
        [SerializeField] public DttPuiDrawer DttPuiDrawer = new DttPuiDrawer();
#endif

#if MPUIKIT_EXISTS
        [SerializeField] public MPUIKitDrawer MPUIKitDrawer = new MPUIKitDrawer();
#endif

#if VECTOR_GRAPHICS_EXISTS
        [SerializeField] public SvgImageDrawer SvgImageDrawer = new SvgImageDrawer();
#endif

#if NOVA_UI_EXISTS
        [SerializeField] public NovaImageDrawer NovaImageDrawer = new NovaImageDrawer();
#endif

#if FLEXIBLE_IMAGE_EXISTS
        [SerializeField] public FlexibleImageDrawer FlexibleImageDrawer = new FlexibleImageDrawer();
#endif
    }
}
#endif