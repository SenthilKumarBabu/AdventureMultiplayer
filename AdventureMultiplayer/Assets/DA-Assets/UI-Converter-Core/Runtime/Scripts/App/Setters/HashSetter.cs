#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public sealed class HashSetter : FcuBase
    {
        public async Task SetHashes(List<Node> fobjects, CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_set_hashes.Localize());

            if (fobjects.IsEmpty())
            {
                return;
            }

            await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                HashEngine engine = new HashEngine(monoBeh.Config);
                engine.BuildIndex(fobjects, token);
                engine.ComputeAllHashes(token);

                for (int i = 0; i < fobjects.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    Node root = fobjects[i];
                    int subtreeHash = engine.GetSubtreeHash(root);
                    int renderHash = engine.GetRenderHash(root);
                    root.Data.ObjectHash = subtreeHash;
                    root.Data.RenderHash = renderHash;
                    fobjects[i] = root;
                }
            }, token);
        }

        private sealed class HashEngine
        {
            private readonly IConvConfig _config;
            private readonly Dictionary<string, Node> _nodesById = new Dictionary<string, Node>(4096);
            private readonly List<Node> _postOrderNodes = new List<Node>(4096);
            private readonly Dictionary<string, HashVariants> _hashVariantsById =
                new Dictionary<string, HashVariants>(4096);
            private readonly Dictionary<string, HashVariants> _renderHashVariantsById =
                new Dictionary<string, HashVariants>(4096);

            public HashEngine(IConvConfig config)
            {
                _config = config;
            }

            public void BuildIndex(List<Node> roots, CancellationToken token)
            {
                Stack<TraversalFrame> stack = new Stack<TraversalFrame>(roots.Count * 2);

                for (int i = 0; i < roots.Count; i++)
                {
                    stack.Push(new TraversalFrame(roots[i], false));
                }

                while (stack.Count > 0)
                {
                    token.ThrowIfCancellationRequested();

                    TraversalFrame frame = stack.Pop();
                    Node node = frame.Node;
                    string id = node.Id;

                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    if (frame.Expanded)
                    {
                        _postOrderNodes.Add(node);
                        continue;
                    }

                    if (_nodesById.ContainsKey(id))
                    {
                        continue;
                    }

                    _nodesById.Add(id, node);
                    stack.Push(new TraversalFrame(node, true));

                    if (node.Children.IsEmpty())
                    {
                        continue;
                    }

                    for (int i = node.Children.Count - 1; i >= 0; i--)
                    {
                        stack.Push(new TraversalFrame(node.Children[i], false));
                    }
                }
            }

            public void ComputeAllHashes(CancellationToken token)
            {
                for (int i = 0; i < _postOrderNodes.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    Node node = _postOrderNodes[i];
                    if (string.IsNullOrEmpty(node.Id))
                    {
                        continue;
                    }

                    int normalHash = ComputeSubtreeHash(node, false);
                    int downloadableHash = ComputeSubtreeHash(node, true);
                    int renderStandaloneHash = ComputeRenderHash(node, false);
                    int renderCompositeHash = ComputeRenderHash(node, true);
                    _hashVariantsById[node.Id] = new HashVariants(normalHash, downloadableHash);
                    _renderHashVariantsById[node.Id] = new HashVariants(renderStandaloneHash, renderCompositeHash);
                }
            }

            public int GetSubtreeHash(Node root)
            {
                if (string.IsNullOrEmpty(root.Id))
                {
                    return 0;
                }

                if (_hashVariantsById.TryGetValue(root.Id, out HashVariants variants))
                {
                    return variants.Get(root.Data != null && root.Data.InsideDownloadable);
                }

                return 0;
            }

            public int GetRenderHash(Node root)
            {
                if (string.IsNullOrEmpty(root.Id))
                {
                    return 0;
                }

                if (_renderHashVariantsById.TryGetValue(root.Id, out HashVariants variants))
                {
                    return variants.Normal;
                }

                return 0;
            }

            private int ComputeSubtreeHash(Node node, bool downloadableContext)
            {
                if (!node.IsVisible())
                {
                    return 0;
                }

                int hash = 0;

                AppendSelfHash(node, downloadableContext, false, ref hash);

                if (node.Children.IsEmpty())
                {
                    return hash;
                }

                if (node.Children.Count > _config.ChildParsingLimit)
                {
                    AddHash(ref hash, nameof(node.Children), node.Children.Count);
                    return hash;
                }

                for (int i = 0; i < node.Children.Count; i++)
                {
                    Node child = node.Children[i];
                    if (!child.IsVisible())
                    {
                        continue;
                    }

                    int childHash = GetChildHash(child, downloadableContext);

                    AddHash(ref hash, "__child_index", i);
                    AddHash(ref hash, "__child_hash", childHash);
                }

                return hash;
            }

            private int ComputeRenderHash(Node node, bool compositeContext)
            {
                if (!node.IsVisible())
                {
                    return 0;
                }

                int hash = 0;

                AppendSelfHash(node, false, true, ref hash);

                if (!compositeContext && ShouldIgnoreChildrenForRenderHash(node))
                {
                    return hash;
                }

                if (node.Children.IsEmpty())
                {
                    return hash;
                }

                if (node.Children.Count > _config.ChildParsingLimit)
                {
                    AddHash(ref hash, nameof(node.Children), node.Children.Count);
                    return hash;
                }

                for (int i = 0; i < node.Children.Count; i++)
                {
                    Node child = node.Children[i];
                    if (!child.IsVisible())
                    {
                        continue;
                    }

                    int childHash = GetChildRenderHash(child, compositeContext: true);

                    AddHash(ref hash, "__child_index", i);
                    AddHash(ref hash, "__child_render_hash", childHash);
                }

                return hash;
            }

            private int GetChildHash(Node child, bool downloadableContext)
            {
                if (string.IsNullOrEmpty(child.Id))
                {
                    return 0;
                }

                if (_hashVariantsById.TryGetValue(child.Id, out HashVariants variants))
                {
                    return variants.Get(downloadableContext);
                }

                return 0;
            }

            private int GetChildRenderHash(Node child, bool compositeContext)
            {
                if (string.IsNullOrEmpty(child.Id))
                {
                    return 0;
                }

                if (_renderHashVariantsById.TryGetValue(child.Id, out HashVariants variants))
                {
                    return variants.Get(compositeContext);
                }

                return 0;
            }

            private void AppendSelfHash(Node fobject, bool downloadableContext, bool renderContext, ref int hash)
            {
                try
                {
                    bool autoSlice9RenderContext = renderContext && fobject.ContainsTag(FcuTag.AutoSlice9);
                    bool scaleInvariantCircleRenderContext = renderContext && fobject.IsScaleInvariantSolidFillCircle();
                    bool scaleInvariantAutoSlice9CircleRenderContext = autoSlice9RenderContext && fobject.IsScaleInvariantAutoSlice9CircleSource();
                    bool scaleInvariantCircleSourceRenderContext = scaleInvariantCircleRenderContext || scaleInvariantAutoSlice9CircleRenderContext;
                    bool scaleInvariantAutoSlice9RenderContext = autoSlice9RenderContext &&
                                                                 !scaleInvariantAutoSlice9CircleRenderContext &&
                                                                 fobject.IsScaleInvariantAutoSlice9();
                    bool autoSlice9SpecificRenderContext = autoSlice9RenderContext && !scaleInvariantAutoSlice9CircleRenderContext;

                    if (scaleInvariantCircleSourceRenderContext)
                    {
                        AddHash(ref hash, "__scale_invariant_solid_filled_circle_source", true);
                    }

                    if (scaleInvariantAutoSlice9RenderContext)
                    {
                        AddHash(ref hash, "__scale_invariant_auto_slice9", true);
                    }

                    if (downloadableContext && fobject.TryGetLocalPosition(out Vector2 rtPos))
                    {
                        AddHash(ref hash, nameof(rtPos), rtPos);
                    }

                    if (!scaleInvariantCircleSourceRenderContext &&
                        !scaleInvariantAutoSlice9RenderContext &&
                        ShouldHashRotation(fobject))
                    {
                        AddHash(ref hash, nameof(DA_Assets.UCC.Extensions.TransformExtensions.GetFigmaRotationAngle), fobject.GetFigmaRotationAngle());
                    }

                    if (!autoSlice9SpecificRenderContext && !scaleInvariantCircleSourceRenderContext && fobject.FillGeometry.IsEmpty() == false)
                    {
                        AddHash(ref hash, nameof(fobject.FillGeometry), fobject.FillGeometry[0].Path.GetDeterministicHashCode());
                    }

                    if (!autoSlice9SpecificRenderContext && !scaleInvariantCircleSourceRenderContext && fobject.StrokeGeometry.IsEmpty() == false)
                    {
                        AddHash(ref hash, nameof(fobject.StrokeGeometry), fobject.StrokeGeometry[0].Path.GetDeterministicHashCode());
                    }

                    if (!scaleInvariantCircleSourceRenderContext && !scaleInvariantAutoSlice9RenderContext)
                    {
                        AddHash(ref hash, nameof(fobject.StrokeWeight), (float)Math.Round(fobject.StrokeWeight, _config.Rounding.StrokeWeight));
                        AddHash(ref hash, nameof(fobject.StrokeAlign), fobject.StrokeAlign);
                    }

                    if (autoSlice9SpecificRenderContext)
                    {
                        AppendAutoSlice9RenderHash(fobject, scaleInvariantAutoSlice9RenderContext, ref hash);
                    }
                    else if (!scaleInvariantCircleSourceRenderContext && fobject.CornerRadiuses.IsEmpty())
                    {
                        AddHash(ref hash, nameof(fobject.CornerRadius), fobject.CornerRadius);
                    }
                    else if (!scaleInvariantCircleSourceRenderContext)
                    {
                        AddHash(ref hash, nameof(fobject.CornerRadiuses), JoinEnumerable(fobject.CornerRadiuses));
                    }
                    AddHash(ref hash, nameof(fobject.CornerSmoothing), fobject.CornerSmoothing);

                    AddHash(ref hash, nameof(fobject.BlendMode), fobject.BlendMode);
                    AddHash(ref hash, nameof(fobject.Opacity), fobject.Opacity);

                    if (!autoSlice9SpecificRenderContext && !scaleInvariantCircleSourceRenderContext)
                    {
                        AddHash(ref hash, nameof(fobject.ClipsContent), fobject.ClipsContent);

                        AddHash(ref hash, nameof(fobject.LayoutMode), fobject.LayoutMode);
                        AddHash(ref hash, nameof(fobject.ItemSpacing), fobject.ItemSpacing);
                        AddHash(ref hash, nameof(fobject.ItemReverseZIndex), fobject.ItemReverseZIndex);
                        AddHash(ref hash, nameof(fobject.PrimaryAxisAlignItems), fobject.PrimaryAxisAlignItems);
                        AddHash(ref hash, nameof(fobject.CounterAxisAlignItems), fobject.CounterAxisAlignItems);

                        AddHash(ref hash, nameof(fobject.PaddingLeft), fobject.PaddingLeft);
                        AddHash(ref hash, nameof(fobject.PaddingRight), fobject.PaddingRight);
                        AddHash(ref hash, nameof(fobject.PaddingTop), fobject.PaddingTop);
                        AddHash(ref hash, nameof(fobject.PaddingBottom), fobject.PaddingBottom);
                        AddHash(ref hash, nameof(fobject.HorizontalPadding), fobject.HorizontalPadding);
                        AddHash(ref hash, nameof(fobject.VerticalPadding), fobject.VerticalPadding);
                    }

                    if (fobject.Type == NodeType.VECTOR)
                    {
                        AddHash(ref hash, nameof(fobject.StrokeCap), fobject.StrokeCap);
                        AddHash(ref hash, nameof(fobject.StrokeJoin), fobject.StrokeJoin);
                        AddHash(ref hash, nameof(fobject.StrokeMiterAngle), fobject.StrokeMiterAngle);

                        if (fobject.StrokeDashes.IsEmpty() == false)
                        {
                            AddHash(ref hash, nameof(fobject.StrokeDashes), JoinEnumerable(fobject.StrokeDashes));
                        }
                    }

                    if (fobject.Type == NodeType.TEXT)
                    {
                        AppendTextStyleHash(fobject.Style, ref hash);
                        AddHash(ref hash, nameof(fobject.Size), GetRoundedSize(fobject));

                        if (downloadableContext)
                        {
                            AddHash(ref hash, nameof(fobject.Characters), fobject.Characters);
                        }
                    }
                    else
                    {
                        if (!autoSlice9SpecificRenderContext && !scaleInvariantCircleSourceRenderContext)
                        {
                            AddHash(ref hash, nameof(fobject.Size), GetRoundedSize(fobject));
                        }
                    }

                    AppendPaintsHash(nameof(fobject.Fills), fobject, fobject.Fills, downloadableContext, ref hash);
                    AppendEffectsHash(nameof(fobject.Effects), fobject, fobject.Effects, downloadableContext, ref hash);
                    if (!scaleInvariantCircleSourceRenderContext && !scaleInvariantAutoSlice9RenderContext)
                    {
                        AppendPaintsHash(nameof(fobject.Strokes), fobject, fobject.Strokes, downloadableContext, ref hash);
                    }
                    if (!scaleInvariantCircleSourceRenderContext)
                    {
                        AppendArcDataHash(nameof(fobject.ArcData), fobject.ArcData, ref hash);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            private void AppendTextStyleHash(Style style, ref int hash)
            {
                try
                {
                    if (style.IsDefault())
                    {
                        return;
                    }

                    AddHash(ref hash, nameof(style.FontFamily), style.FontFamily);
                    AddHash(ref hash, nameof(style.FontWeight), style.FontWeight);
                    AddHash(ref hash, nameof(style.FontSize), style.FontSize);
                    AddHash(ref hash, nameof(style.TextAlignHorizontal), style.TextAlignHorizontal);
                    AddHash(ref hash, nameof(style.TextAlignVertical), style.TextAlignVertical);
                    AddHash(ref hash, nameof(style.LetterSpacing), style.LetterSpacing);
                    AddHash(ref hash, nameof(style.LineHeightPx), style.LineHeightPx);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            private void AppendPaintsHash(string effectName, Node fobject, List<Paint> paints, bool downloadableContext, ref int hash)
            {
                if (paints.IsEmpty())
                {
                    return;
                }

                if (!paints.Any(x => x.IsVisible()))
                {
                    return;
                }

                AddHash(ref hash, "__effect_group_begin", effectName);

                foreach (Paint item in paints)
                {
                    try
                    {
                        if (!item.IsVisible())
                        {
                            continue;
                        }

                        AddHash(ref hash, nameof(item.Type), item.Type);
                        AddHash(ref hash, nameof(item.Opacity), item.Opacity);

                        if (downloadableContext)
                        {
                            AddHash(ref hash, nameof(item.Color), item.Color);
                        }
                        else if (fobject.Data.Graphic.HasSingleColor && fobject.Type != NodeType.TEXT)
                        {





                            AddHash(ref hash, nameof(item.Color), Color.white);
                        }
                        else
                        {
                            AddHash(ref hash, nameof(item.Color), item.Color);
                        }

                        AddHash(ref hash, nameof(item.BlendMode), item.BlendMode);
                        AddHash(ref hash, nameof(item.ScaleMode), item.ScaleMode);
                        AddHash(ref hash, nameof(item.ScalingFactor), item.ScalingFactor);

                        if (item.Rotation != null)
                        {
                            AddHash(ref hash, nameof(item.Rotation), (float)Math.Round(item.Rotation.Value, _config.Rounding.Rotation));
                        }

                        AddHash(ref hash, nameof(item.ImageRef), item.ImageRef);
                        AddHash(ref hash, nameof(item.GifRef), item.GifRef);

                        AddHash(ref hash, nameof(item.Filters.Exposure), item.Filters.Exposure);
                        AddHash(ref hash, nameof(item.Filters.Contrast), item.Filters.Contrast);
                        AddHash(ref hash, nameof(item.Filters.Saturation), item.Filters.Saturation);
                        AddHash(ref hash, nameof(item.Filters.Temperature), item.Filters.Temperature);
                        AddHash(ref hash, nameof(item.Filters.Tint), item.Filters.Tint);
                        AddHash(ref hash, nameof(item.Filters.Highlights), item.Filters.Highlights);
                        AddHash(ref hash, nameof(item.Filters.Shadows), item.Filters.Shadows);

                        if (item.ImageTransform.IsEmpty() == false)
                        {
                            AddHash(ref hash, nameof(item.ImageTransform), FlattenMatrix(item.ImageTransform));
                        }

                        if (item.GradientStops.IsEmpty() == false)
                        {
                            foreach (GradientStop gs in item.GradientStops)
                            {
                                if (downloadableContext)
                                {
                                    AddHash(ref hash, nameof(gs.Color), gs.Color);
                                }
                                else if (fobject.Data.Graphic.HasSingleColor && fobject.Type != NodeType.TEXT)
                                {
                                    AddHash(ref hash, nameof(gs.Color), Color.white);
                                }
                                else
                                {
                                    AddHash(ref hash, nameof(gs.Color), gs.Color);
                                }

                                AddHash(ref hash, nameof(gs.Position), (float)Math.Round(gs.Position, _config.Rounding.Position));
                            }
                        }

                        if (item.GradientHandlePositions.IsEmpty() == false)
                        {
                            AddHash(ref hash, nameof(item.GradientHandlePositions), JoinEnumerable(item.GradientHandlePositions));
                        }
                    }
                    catch (Exception ex)
                    {
                        FcuLogger.Debug(ex, FcuDebugSettingsFlags.LogError);
                    }
                }

                AddHash(ref hash, "__effect_group_end", effectName);
            }

            private void AppendEffectsHash(string effectName, Node fobject, List<Effect> effects, bool downloadableContext, ref int hash)
            {
                if (effects.IsEmpty())
                {
                    return;
                }

                if (!effects.Any(x => x.IsVisible()))
                {
                    return;
                }

                AddHash(ref hash, "__effect_group_begin", effectName);

                foreach (Effect item in effects)
                {
                    try
                    {
                        if (!item.IsVisible())
                        {
                            continue;
                        }

                        AddHash(ref hash, nameof(item.Type), item.Type);
                        AddHash(ref hash, nameof(item.Radius), item.Radius);

                        if (downloadableContext)
                        {
                            AddHash(ref hash, nameof(item.Color), item.Color);
                        }
                        else if (fobject.Data.Graphic.HasSingleColor && fobject.Type != NodeType.TEXT)
                        {
                            AddHash(ref hash, nameof(item.Color), Color.white);
                        }
                        else
                        {
                            AddHash(ref hash, nameof(item.Color), item.Color);
                        }

                        AddHash(ref hash, nameof(item.BlendMode), item.BlendMode);
                        AddHash(ref hash, nameof(item.Offset), item.Offset);
                        AddHash(ref hash, nameof(item.Spread), item.Spread);
                        AddHash(ref hash, nameof(item.ShowShadowBehindNode), item.ShowShadowBehindNode);
                    }
                    catch (Exception ex)
                    {
                        FcuLogger.Debug(ex, FcuDebugSettingsFlags.LogError);
                    }
                }

                AddHash(ref hash, "__effect_group_end", effectName);
            }

            private void AppendArcDataHash(string effectName, ArcData arcData, ref int hash)
            {
                try
                {
                    AddHash(ref hash, "__effect_group_begin", effectName);
                    AddHash(ref hash, nameof(arcData.StartingAngle), arcData.StartingAngle);
                    AddHash(ref hash, nameof(arcData.EndingAngle), arcData.EndingAngle);
                    AddHash(ref hash, nameof(arcData.InnerRadius), arcData.InnerRadius);
                    AddHash(ref hash, "__effect_group_end", effectName);
                }
                catch (Exception ex)
                {
                    FcuLogger.Debug(ex, FcuDebugSettingsFlags.LogError);
                }
            }

            private void AppendAutoSlice9RenderHash(Node fobject, bool scaleInvariant, ref int hash)
            {
                AddHash(ref hash, "__auto_slice9", true);

                if (scaleInvariant)
                    return;

                if (fobject.TryGetAutoSlice9SourceBorder(out Vector4 border))
                {
                    AddHash(ref hash, "__auto_slice9_border", JoinRounded(border.x, border.y, border.z, border.w));
                }
            }

            private static bool ShouldHashRotation(Node fobject)
            {
                return !fobject.IsRotationInvariantSolidCircle();
            }

            private static bool ShouldIgnoreChildrenForRenderHash(Node fobject)
            {
                return fobject.IsGenerativeType();
            }

            private Vector2 GetRoundedSize(Node fobject)
            {
                return new Vector2(
                    (float)Math.Round(fobject.Size.x, _config.Rounding.BoundingSize),
                    (float)Math.Round(fobject.Size.y, _config.Rounding.BoundingSize));
            }

            private static void GetCornerRadii(
                Node fobject,
                out float topLeft,
                out float topRight,
                out float bottomRight,
                out float bottomLeft)
            {
                if (fobject.CornerRadiuses.IsEmpty())
                {
                    float radius = fobject.CornerRadius.ToFloat();
                    topLeft = radius;
                    topRight = radius;
                    bottomRight = radius;
                    bottomLeft = radius;
                    return;
                }

                topLeft = fobject.CornerRadiuses[0];
                topRight = fobject.CornerRadiuses.Count > 1 ? fobject.CornerRadiuses[1] : topLeft;
                bottomRight = fobject.CornerRadiuses.Count > 2 ? fobject.CornerRadiuses[2] : topRight;
                bottomLeft = fobject.CornerRadiuses.Count > 3 ? fobject.CornerRadiuses[3] : bottomRight;
            }

            private static float GetMaxAutoSliceEffectExtent(Node fobject)
            {
                if (fobject.Effects.IsEmpty())
                    return 0f;

                float maxEffectExtent = 0f;

                foreach (Effect effect in fobject.Effects)
                {
                    if (!effect.IsVisible())
                        continue;

                    switch (effect.Type)
                    {
                        case EffectType.DROP_SHADOW:
                        case EffectType.INNER_SHADOW:
                            maxEffectExtent = Mathf.Max(maxEffectExtent, effect.Radius + (effect.Spread ?? 0f));
                            break;
                        case EffectType.LAYER_BLUR:
                            maxEffectExtent = Mathf.Max(maxEffectExtent, effect.Radius);
                            break;
                    }
                }

                return maxEffectExtent;
            }

            private string JoinRounded(params float[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = (float)Math.Round(values[i], _config.Rounding.BoundingSize);
                }

                return JoinEnumerable(values);
            }

            private static void AddHash(ref int hash, string name, object value)
            {
                if (value == null)
                {
                    return;
                }

                string text = value.ToString();
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                unchecked
                {
                    hash = (hash * 397) ^ name.GetDeterministicHashCode();
                    hash = (hash * 397) ^ text.GetDeterministicHashCode();
                }
            }

            private static string FlattenMatrix<T>(IReadOnlyList<IReadOnlyList<T>> matrix)
            {
                if (matrix == null || matrix.Count == 0)
                {
                    return string.Empty;
                }

                StringBuilder sb = StringBuilderPool.Get();
                try
                {
                    bool first = true;

                    for (int r = 0; r < matrix.Count; r++)
                    {
                        IReadOnlyList<T> row = matrix[r];
                        if (row == null)
                        {
                            continue;
                        }

                        for (int c = 0; c < row.Count; c++)
                        {
                            if (first == false)
                            {
                                sb.Append(' ');
                            }

                            sb.Append(row[c]);
                            first = false;
                        }
                    }

                    return sb.ToString();
                }
                finally
                {
                    StringBuilderPool.Return(sb);
                }
            }

            private static string JoinEnumerable<T>(IEnumerable<T> values)
            {
                if (values == null)
                {
                    return string.Empty;
                }

                StringBuilder sb = StringBuilderPool.Get();
                try
                {
                    bool first = true;

                    foreach (T item in values)
                    {
                        if (first == false)
                        {
                            sb.Append(' ');
                        }

                        sb.Append(item);
                        first = false;
                    }

                    return sb.ToString();
                }
                finally
                {
                    StringBuilderPool.Return(sb);
                }
            }

            private readonly struct TraversalFrame
            {
                public readonly Node Node;
                public readonly bool Expanded;

                public TraversalFrame(Node node, bool expanded)
                {
                    Node = node;
                    Expanded = expanded;
                }
            }

            private readonly struct HashVariants
            {
                public readonly int Normal;
                public readonly int Downloadable;

                public HashVariants(int normal, int downloadable)
                {
                    Normal = normal;
                    Downloadable = downloadable;
                }

                public int Get(bool downloadableContext)
                {
                    return downloadableContext ? Downloadable : Normal;
                }
            }
        }

        private static class StringBuilderPool
        {
            [ThreadStatic]
            private static StringBuilder _cached;

            public static StringBuilder Get()
            {
                StringBuilder sb = _cached;
                if (sb != null)
                {
                    _cached = null;
                    sb.Clear();
                    return sb;
                }

                return new StringBuilder(256);
            }

            public static void Return(StringBuilder sb)
            {
                if (sb == null)
                {
                    return;
                }

                if (sb.Capacity > 16 * 1024)
                {
                    return;
                }

                sb.Clear();
                _cached = sb;
            }
        }
    }
}
#endif