#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

#if NOVA_UI_EXISTS
using Nova;
#endif

#pragma warning disable CS1998

namespace DA_Assets.UCC
{
    [Serializable]
    public class TransformSetter : FcuBase
    {
        public float GetAbsoluteAngle(Node fobject)
        {
            if (!ReferenceEquals(fobject, null) && fobject.Data != null && fobject.Data.HasTransformComputationCache)
            {
                return fobject.Data.CachedAbsoluteFigmaRotationAngle;
            }

            float totalAngle = 0;

            Node current = fobject;

            while (true)
            {
                totalAngle += current.GetFigmaRotationAngle();

                if (!monoBeh.CurrentProject.TryGetParent(current, out current))
                {
                    break;
                }
            }

            return totalAngle;
        }

        public async Task SetTransformPos(List<Node> fobjects)
        {
            foreach (Node fobject in fobjects)
            {
                if (fobject.Data?.RectGameObject == null)
                {
                    continue;
                }

                RectTransform rt = fobject.Data.RectGameObject.GetComponent<RectTransform>();
                rt.SetSmartAnchor(AnchorType.BottomLeft);
                rt.SetSmartPivot(PivotType.TopLeft);

                fobject.Data.FRect = GetGlobalRect(fobject);

                rt.sizeDelta = fobject.Data.FRect.size;
                rt.anchoredPosition = fobject.Data.FRect.position;

                rt.SetSmartPivot(PivotType.MiddleCenter);
                rt.SetRotation(fobject.Data.FRect.absoluteAngle);

                if (fobject.ContainsTag(FcuTag.Frame))
                {
                    rt.SetSmartAnchor(AnchorType.TopLeft);
                }
                else if (!fobject.Data.Parent.ContainsTag(FcuTag.AutoLayoutGroup))
                {
                    rt.SetSmartAnchor(fobject.GetFigmaAnchor());
                }

                rt.SetSmartPivot(monoBeh.Settings.MainSettings.PivotType);
            }
        }

        public FRect GetGlobalRect(Node fobject)
        {
            if (!ReferenceEquals(fobject, null) && fobject.Data != null && fobject.Data.HasCachedGlobalRect)
            {
                return fobject.Data.FRect;
            }

            FRect rect = new FRect();
            Vector2 position = new Vector2();
            Vector2 size = new Vector2();

            fobject.GetBoundingSize(out Vector2 bSize);
            fobject.GetBoundingPosition(out Vector2 bPos);








            bool isSprite = fobject.IsSprite();
            rect.angle = isSprite
                ? 0
                : (fobject.Data.HasTransformComputationCache ? fobject.Data.CachedMatrixAngle : fobject.GetAngleFromMatrix());
            rect.absoluteAngle = isSprite ? 0 : GetAbsoluteAngle(fobject);

            bool uguiOrNova = monoBeh.IsUGUI() || monoBeh.IsNova();

            int state = 0;

            if (isSprite)
            {
                bool hasRenderSize = fobject.GetRenderSize(out Vector2 rSize);
                bool hasRenderPos = fobject.GetRenderPosition(out Vector2 rPos);
                bool ancestorClips = HasAncestorWithClipContent(fobject);
                bool clipsContent = fobject.ClipsContent.ToBoolNullFalse();

                bool clippedRenderBounds = HasClippedRenderBounds(fobject);
                bool hasUsableRenderBounds = hasRenderSize && hasRenderPos && !clippedRenderBounds;
                bool ancestorClipsRenderBounds = ancestorClips && fobject.Type != NodeType.TEXT;
                bool canUseRenderBounds = hasUsableRenderBounds && !ancestorClipsRenderBounds;
                bool canUseRenderBoundsDirectly = canUseRenderBounds && !clipsContent;

#if IMAGE_OVERFLOW_EXISTS
                if (hasUsableRenderBounds &&
                    ImageOverflowUtility.ShouldUseImageOverflow(fobject, monoBeh))
                {
                    state = 7;



                    size = bSize;
                    position = bPos;
                }
                else
#endif
                if (canUseRenderBoundsDirectly)
                {
                    state = 1;



                    size = rSize;
                    position = rPos;
                }
                else
                {








                    Vector4 expansion;

                    bool hasOwnEffects = fobject.Effects != null && fobject.Effects.Count > 0;

                    if (hasOwnEffects)
                    {

                        expansion = CalculateEffectExpansion(fobject.Effects);
                    }
                    else
                    {

                        expansion = CalculateEffectExpansionRecursive(fobject);
                    }

                    if (fobject.IsArcDataFilled())
                    {
                        state = 2;


                        ComputeArcAABB(fobject, bPos, bSize, out Vector2 arcPos, out Vector2 arcSize);
                        size = new Vector2(
                            arcSize.x + expansion.x + expansion.z,
                            arcSize.y + expansion.y + expansion.w);
                        position = new Vector2(
                            arcPos.x - expansion.x,
                            arcPos.y - expansion.y);
                    }
                    else
                    {
                        state = 3;

                        size = new Vector2(
                            bSize.x + expansion.x + expansion.z,
                            bSize.y + expansion.y + expansion.w);
                        position = new Vector2(
                            bPos.x - expansion.x,
                            bPos.y - expansion.y);
                    }
                }
            }
            else
            {
                state = 4;



                size = fobject.Size;
                position = new Vector2(
                    bPos.x + (bSize.x - size.x) / 2f,
                    bPos.y + (bSize.y - size.y) / 2f);
            }

            if (fobject.TryFixSizeWithStroke(size.y, out float newY))
            {
                size.y = newY;
            }

            FcuLogger.Debug($"{nameof(GetGlobalRect)} | {fobject.Data.NameHierarchy} | state: {state} | {size} | {position} | {rect.absoluteAngle}", FcuDebugSettingsFlags.LogTransform);

            rect.size = size;
            rect.position = new Vector2(position.x, (uguiOrNova ? -position.y : position.y));

            List<Node> layoutChildren = new List<Node>();
            foreach (int index in fobject.Data.ChildIndexes)
            {
                if (monoBeh.CurrentProject.TryGetByIndex(index, out Node child))
                {
                    GetGlobalRect(child);
                    layoutChildren.Add(child);
                }
            }

            rect.padding = GetPadding(fobject, monoBeh.Config).AdjustPadding(fobject, rect, layoutChildren);

            fobject.Data.FRect = rect;
            fobject.Data.HasCachedGlobalRect = true;

            return rect;
        }

        private static Vector4 CalculateEffectExpansionRecursive(Node fobject)
        {
            if (fobject.Children.IsEmpty())
                return Vector4.zero;


            if (fobject.ClipsContent.ToBoolNullFalse() || fobject.Children.Any(c => c.IsObjectMask()))
                return Vector4.zero;

            float bLeft   = fobject.AbsoluteBoundingBox.X ?? 0f;
            float bTop    = fobject.AbsoluteBoundingBox.Y ?? 0f;
            float bRight  = bLeft + (fobject.AbsoluteBoundingBox.Width  ?? 0f);
            float bBottom = bTop  + (fobject.AbsoluteBoundingBox.Height ?? 0f);


            float minLeft   = bLeft;
            float minTop    = bTop;
            float maxRight  = bRight;
            float maxBottom = bBottom;

            foreach (Node child in fobject.Children)
                CollectRenderEdges(child, ref minLeft, ref minTop, ref maxRight, ref maxBottom);

            return new Vector4(
                Mathf.Max(0, bLeft   - minLeft),
                Mathf.Max(0, bTop    - minTop),
                Mathf.Max(0, maxRight  - bRight),
                Mathf.Max(0, maxBottom - bBottom));
        }

        private static void CollectRenderEdges(
            Node fobject,
            ref float minLeft, ref float minTop, ref float maxRight, ref float maxBottom)
        {
            float bLeft   = fobject.AbsoluteBoundingBox.X ?? 0f;
            float bTop    = fobject.AbsoluteBoundingBox.Y ?? 0f;
            float bRight  = bLeft + (fobject.AbsoluteBoundingBox.Width  ?? 0f);
            float bBottom = bTop  + (fobject.AbsoluteBoundingBox.Height ?? 0f);

            Vector4 exp = CalculateEffectExpansion(fobject.Effects);


            minLeft   = Mathf.Min(minLeft,   bLeft   - exp.x);
            minTop    = Mathf.Min(minTop,     bTop    - exp.y);
            maxRight  = Mathf.Max(maxRight,  bRight  + exp.z);
            maxBottom = Mathf.Max(maxBottom, bBottom + exp.w);

            if (fobject.Children.IsEmpty())
                return;

            if (fobject.ClipsContent.ToBoolNullFalse())
                return;


            if (fobject.Children.Any(c => c.IsObjectMask()))
                return;

            foreach (Node child in fobject.Children)
                CollectRenderEdges(child, ref minLeft, ref minTop, ref maxRight, ref maxBottom);
        }


        public static Vector4 CalculateEffectExpansion(List<Effect> effects)
        {
            if (effects == null || effects.Count == 0)
                return Vector4.zero;

            float left = 0, top = 0, right = 0, bottom = 0;

            foreach (Effect effect in effects)
            {
                if (effect.Visible.HasValue && !effect.Visible.Value)
                    continue;

                float eLeft = 0, eTop = 0, eRight = 0, eBottom = 0;

                switch (effect.Type)
                {
                    case EffectType.DROP_SHADOW:
                        {
                            float radius = effect.Radius;
                            float spread = effect.Spread ?? 0f;
                            float extent = radius + spread;

                            eLeft = Mathf.Max(0, extent - effect.Offset.x);
                            eRight = Mathf.Max(0, extent + effect.Offset.x);
                            eTop = Mathf.Max(0, extent - effect.Offset.y);
                            eBottom = Mathf.Max(0, extent + effect.Offset.y);
                            break;
                        }
                    case EffectType.LAYER_BLUR:
                        {
                            eLeft = eRight = eTop = eBottom = effect.Radius;
                            break;
                        }

                }


                left = Mathf.Max(left, eLeft);
                top = Mathf.Max(top, eTop);
                right = Mathf.Max(right, eRight);
                bottom = Mathf.Max(bottom, eBottom);
            }

            return new Vector4(left, top, right, bottom);
        }

        private bool HasAncestorWithClipContent(Node fobject)
        {
            int parentIndex = fobject.Data.ParentIndex;
            while (parentIndex >= 0 && monoBeh.CurrentProject.TryGetByIndex(parentIndex, out Node parent))
            {
                if (parent.ClipsContent.ToBoolNullFalse())
                    return true;
                parentIndex = parent.Data.ParentIndex;
            }
            return false;
        }

        private static bool HasClippedRenderBounds(Node fobject)
        {
            if (fobject.IsArcDataFilled())
                return false;

            if (fobject.Type == NodeType.TEXT)
                return false;

            if (!TryGetBounds(fobject.AbsoluteBoundingBox, out Rect boundingRect) ||
                !TryGetBounds(fobject.AbsoluteRenderBounds, out Rect renderRect))
                return false;

            const float tolerance = 0.5f;

            return renderRect.xMin > boundingRect.xMin + tolerance ||
                   renderRect.yMin > boundingRect.yMin + tolerance ||
                   renderRect.xMax < boundingRect.xMax - tolerance ||
                   renderRect.yMax < boundingRect.yMax - tolerance;
        }

        private static bool TryGetBounds(BoundingBox box, out Rect rect)
        {
            rect = default;

            if (!box.X.HasValue ||
                !box.Y.HasValue ||
                !box.Width.HasValue ||
                !box.Height.HasValue)
                return false;

            rect = new Rect(box.X.Value, box.Y.Value, box.Width.Value, box.Height.Value);
            return true;
        }

        private bool HasRotatedAncestor(Node fobject)
        {
            if (!ReferenceEquals(fobject, null) && fobject.Data != null && fobject.Data.HasTransformComputationCache)
            {
                return fobject.Data.CachedHasRotatedAncestor;
            }

            int parentIndex = fobject.Data.ParentIndex;

            while (parentIndex >= 0 && monoBeh.CurrentProject.TryGetByIndex(parentIndex, out Node parent))
            {
                if (Mathf.Abs(parent.GetAngleFromMatrix()) > 0.001f)
                    return true;

                parentIndex = parent.Data.ParentIndex;
            }

            return false;
        }

        private bool HasSelfOrAncestorRotation(Node fobject)
        {
            float selfAngle = fobject.Data.HasTransformComputationCache
                ? fobject.Data.CachedMatrixAngle
                : fobject.GetAngleFromMatrix();

            if (Mathf.Abs(selfAngle) > 0.001f)
                return true;

            return HasRotatedAncestor(fobject);
        }

        private static void ComputeArcAABB(
            Node fobject,
            Vector2 bPos, Vector2 bSize,
            out Vector2 arcPos, out Vector2 arcSize)
        {
            float rx = fobject.Size.x / 2f;
            float ry = fobject.Size.y / 2f;
            float startAngle = fobject.ArcData.StartingAngle;
            float endAngle = fobject.ArcData.EndingAngle;
            float innerR = fobject.ArcData.InnerRadius;
            float rotRad = Mathf.Deg2Rad * fobject.GetFigmaRotationAngle();


            List<Vector2> points = new List<Vector2>(12);


            points.Add(new Vector2(rx * Mathf.Cos(startAngle), ry * Mathf.Sin(startAngle)));
            points.Add(new Vector2(rx * Mathf.Cos(endAngle), ry * Mathf.Sin(endAngle)));


            if (innerR > 0f)
            {
                float irx = innerR * rx;
                float iry = innerR * ry;
                points.Add(new Vector2(irx * Mathf.Cos(startAngle), iry * Mathf.Sin(startAngle)));
                points.Add(new Vector2(irx * Mathf.Cos(endAngle), iry * Mathf.Sin(endAngle)));
                AddExtremaIfInRange(points, irx, iry, startAngle, endAngle);
            }
            else
            {
                points.Add(Vector2.zero);
            }


            AddExtremaIfInRange(points, rx, ry, startAngle, endAngle);


            float cosR = Mathf.Cos(rotRad);
            float sinR = Mathf.Sin(rotRad);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (Vector2 p in points)
            {
                float rotX = p.x * cosR - p.y * sinR;
                float rotY = p.x * sinR + p.y * cosR;

                minX = Mathf.Min(minX, rotX);
                maxX = Mathf.Max(maxX, rotX);
                minY = Mathf.Min(minY, rotY);
                maxY = Mathf.Max(maxY, rotY);
            }


            Vector2 center = new Vector2(bPos.x + bSize.x / 2f, bPos.y + bSize.y / 2f);
            arcPos = new Vector2(center.x + minX, center.y + minY);
            arcSize = new Vector2(maxX - minX, maxY - minY);
        }

        private static void AddExtremaIfInRange(
            List<Vector2> points, float rx, float ry,
            float startAngle, float endAngle)
        {

            float[] cardinals = { 0f, Mathf.PI / 2f, Mathf.PI, 3f * Mathf.PI / 2f };
            foreach (float angle in cardinals)
            {
                if (IsAngleInRange(angle, startAngle, endAngle))
                {
                    points.Add(new Vector2(rx * Mathf.Cos(angle), ry * Mathf.Sin(angle)));
                }
            }
        }

        private static bool IsAngleInRange(float angle, float start, float end)
        {
            float twoPi = 2f * Mathf.PI;
            float normalized = angle;
            while (normalized < start) normalized += twoPi;
            return normalized <= end;
        }

        private static RectOffsetCustom GetPadding(Node fobject, IConvConfig config)
        {
            return new RectOffsetCustom
            {
                bottom = (int)fobject.PaddingBottom.ToFloat().Round(config.Rounding.Padding),
                top = (int)fobject.PaddingTop.ToFloat().Round(config.Rounding.Padding),
                left = (int)fobject.PaddingLeft.ToFloat().Round(config.Rounding.Padding),
                right = (int)fobject.PaddingRight.ToFloat().Round(config.Rounding.Padding)
            };
        }


        public async Task RestoreParentsRect(List<Node> fobjects)
        {
            foreach (Node fobject in fobjects)
            {
                if (fobject.Data?.RectGameObject == null)
                {
                    continue;
                }

                if (!fobject.ContainsTag(FcuTag.Frame))
                {
                    if (fobject.Data.RectGameObject != null)
                    {
                        fobject.Data.RectGameObject.transform.SetParent(fobject.Data.ParentTransformRect);
                    }
                    else
                    {
                        fobject.Data.RectGameObject.transform.SetParent(monoBeh.transform);
                    }
                }
            }
        }

        public async Task RestoreParents(List<Node> fobjects)
        {
            foreach (Node fobject in fobjects)
            {
                if (fobject.Data?.GameObject == null)
                {
                    continue;
                }

                if (fobject.Data.ParentTransform != null)
                {
                    fobject.Data.GameObject.transform.SetParent(fobject.Data.ParentTransform);
                }
                else
                {
                    fobject.Data.GameObject.transform.SetParent(monoBeh.transform);
                }
            }
        }

        internal async Task ApplyFigmaScaleAnchors(List<Node> fobjects)
        {
            foreach (Node fobject in fobjects)
            {
                if (fobject.Data?.GameObject == null)
                    continue;

                if (fobject.Data.ParentIndex < 0)
                    continue;

                if (fobject.Data.Parent.ContainsTag(FcuTag.AutoLayoutGroup))
                    continue;

                if (!fobject.HasFigmaScaleConstraint())
                    continue;

                if (!fobject.Data.GameObject.TryGetComponentSafe(out RectTransform rectTransform))
                    continue;

                rectTransform.ApplyFigmaScaleAnchors(fobject);
            }

            await Task.Yield();
        }

        internal async Task MoveUguiTransforms(List<Node> currPage)
        {
            foreach (Node fobject in currPage)
            {
                if (fobject.Data.GameObject == null)
                    continue;

                if (fobject.Data.RectGameObject == null)
                    continue;

                fobject.Data.GameObject.TryAddComponent(out RectTransform goRt);
                fobject.Data.RectGameObject.TryGetComponentSafe(out RectTransform rectRt);

                goRt.CopyFrom(rectRt);
            }

            await Task.Yield();
        }

        internal void MoveNovaTransforms(List<Node> currPage)
        {
            Transform tempParent = MonoBehExtensions.CreateEmptyGameObject(nameof(tempParent), monoBeh.transform).transform;

            foreach (Node fobject in currPage)
            {
                if (fobject.Data.GameObject == null)
                    continue;

                if (fobject.Data.RectGameObject == null)
                    continue;

                fobject.Data.RectGameObject.TryGetComponentSafe(out RectTransform rectRt);
                fobject.Data.UguiTransformData = UguiTransformData.Create(rectRt);

#if NOVA_UI_EXISTS
                if (fobject.ContainsTag(FcuTag.Text))
                {
                    fobject.Data.GameObject.TryAddComponent(out TextBlock textBlock);
                }
                else
                {
                    fobject.Data.GameObject.TryAddComponent(out UIBlock2D uiBlock2d);
                }

                UIBlock uiBlock = fobject.Data.GameObject.GetComponent<UIBlock>();
                uiBlock.Color = default;

                uiBlock.Layout.Size = new Length3
                {
                    X = fobject.Data.FRect.size.x,
                    Y = fobject.Data.FRect.size.y,
                };

                fobject.ExecuteWithTemporaryParent(tempParent, x => x.Data.GameObject, () =>
                {
                    SetFigmaRotation(fobject, fobject.Data.GameObject);
                });

                uiBlock.Layout.Position = new Length3
                {
                    X = fobject.Data.UguiTransformData.LocalPosition.x,
                    Y = fobject.Data.UguiTransformData.LocalPosition.y,
                };
#endif
            }

            tempParent.gameObject.Destroy();
        }

        public async Task SetNovaAnchors(List<Node> fobjects, CancellationToken token)
        {
            int total = fobjects.Count;
            int processed = 0;

            IEnumerable<FrameGroup> nodesByFrame = fobjects
                .GroupBy(x => x.Data.RootFrame)
                .Select(g => new FrameGroup
                {
                    Childs = g.Select(x => x).ToList(),
                    RootFrame = g.First()
                });

            foreach (FrameGroup rootFrame in nodesByFrame)
            {
                if (rootFrame.RootFrame.Data.RectGameObject == null)
                    continue;

                _ = SetNovaAnchorsRoutine(rootFrame.Childs, () => processed++, token);
            }

            int tempCount = -1;
            while (FcuLogger.WriteLogBeforeEqual(
                ref processed,
                ref total,
                FcuLocKey.log_set_anchors.Localize(processed, total),
                ref tempCount))
            {
                await Task.Delay(1000, token);
            }
        }

        private async Task SetNovaAnchorsRoutine(List<Node> fobjects, Action onProcess, CancellationToken token)
        {
#if NOVA_UI_EXISTS
            foreach (Node fobject in fobjects)
            {
                if (fobject.Data.GameObject == null)
                    continue;

                fobject.Data.GameObject.TryGetComponentSafe(out UIBlock uiBlock);
                await uiBlock.SetNovaAnchor(fobject.GetFigmaAnchor(), token);

                onProcess.Invoke();
            }
#endif

            await Task.Yield();
        }

        internal async Task RestoreNovaFramePositions(List<Node> fobjects, CancellationToken token)
        {
            IEnumerable<FrameGroup> nodesByFrame = fobjects
                .GroupBy(x => x.Data.RootFrame)
                .Select(g => new FrameGroup
                {
                    Childs = g.Select(x => x).ToList(),
                    RootFrame = g.First()
                });

            foreach (FrameGroup rootFrame in nodesByFrame)
            {
                if (rootFrame.RootFrame.Data.GameObject == null)
                    continue;

#if NOVA_UI_EXISTS
                rootFrame.RootFrame.Data.GameObject.TryGetComponentSafe(out UIBlock uiBlock);

                await uiBlock.SetNovaAnchor(AnchorType.TopLeft, token);
#endif
                await Task.Delay(100, token);

#if NOVA_UI_EXISTS
                uiBlock.Layout.Position = new Length3
                {
                    X = rootFrame.RootFrame.AbsoluteBoundingBox.X.ToFloat(),
                    Y = rootFrame.RootFrame.AbsoluteBoundingBox.Y.ToFloat(),
                };
#endif
            }
        }

        private void SetFigmaRotation(Node fobject, GameObject target)
        {
            Transform rect = target.GetComponent<Transform>();
            rect.SetRotation(fobject.Data.FRect.absoluteAngle);
        }

        internal async Task SetSiblingIndex(List<Node> fobjects)
        {
            foreach (var item in fobjects)
            {
                if (item.Data.GameObject == null)
                {
                    continue;
                }

                item.Data.GameObject.transform.SetSiblingIndex(item.Data.SiblingIndex);
            }
        }

        internal async Task SetStretchAllIfNeeded(List<Node> fobjects)
        {
            if (monoBeh.Settings.MainSettings.PositioningMode == PositioningMode.GameView)
            {
                var frames = fobjects
                    .Where(x => x.ContainsTag(FcuTag.Frame));

                await Task.Yield();

                var frameSizeGroups = frames
                    .GroupBy(x => x.Size)
                    .Select(group => new
                    {
                        Size = group.Key,
                        Count = group.Count()
                    });

                await Task.Yield();

                var mostCommonSize = frameSizeGroups
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefault();

                if (mostCommonSize.Size.x > 0 && mostCommonSize.Size.y > 0)
                {
                    monoBeh.EditorDelegateHolder.SetGameViewSize(mostCommonSize.Size);
                }

                foreach (Node frame in frames)
                {
                    if (frame.Data.GameObject == null)
                        continue;

                    RectTransform rt = frame.Data.GameObject.GetComponent<RectTransform>();

                    rt.SetSmartAnchor(AnchorType.StretchAll);
                    rt.offsetMin = new Vector2(0, 0);
                    rt.offsetMax = new Vector2(0, 0);
                    rt.localScale = Vector3.one;
                }
            }
        }
    }
}
#endif