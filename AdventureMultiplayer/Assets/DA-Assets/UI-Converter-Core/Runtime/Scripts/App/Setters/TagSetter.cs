#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class TagSetter : FcuBase
    {
        private const int MAX_THREAD_COUNT = 10;
        private static readonly FcuTag[] AllTags = Enum.GetValues(typeof(FcuTag)).Cast<FcuTag>().ToArray();
        private static readonly FcuTag[] ManualTagCandidates = AllTags.Where(x => x != FcuTag.None).ToArray();

        private int _outstandingTasks = 0;
        private int _currentConcurrency = 0;
        private int _totalNodes;
        private int _processedElementsAll = 0;
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
        private Dictionary<string, bool> _singleImageInsideCache = new Dictionary<string, bool>();
        private Dictionary<string, bool> _singleImageSubtreeCache = new Dictionary<string, bool>();

        public Dictionary<FcuTag, int> TagsCounter { get; set; } = new Dictionary<FcuTag, int>();

        private void InitializeTagsCounter()
        {
            foreach (FcuTag tag in AllTags)
            {
                TagsCounter[tag] = 0;
            }
        }

        public async Task SetTags(Node page, CancellationToken token)
        {
            InitializeTagsCounter();
            ClearSingleImageCaches();

            _totalNodes = CountAllNodes(page);
            _processedElementsAll = 0;

            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.Tagging, _totalNodes, false);

            Debug.Log(FcuLocKey.log_tagging_by_parts.Localize(1));
            await SetTagsAsync(page, TagAlgorithm.Figma, token);

            BuildSingleImageCaches(page);

            Debug.Log(FcuLocKey.log_tagging_by_parts.Localize(2));
            await SetTagsAsync(page, TagAlgorithm.Smart, token);

            Debug.Log(FcuLocKey.log_tagging_by_parts.Localize(3));
            await SetTagsAsync(page, TagAlgorithm.Ignore, token);

            monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.Tagging);
        }

        private void ClearSingleImageCaches()
        {
            _singleImageInsideCache.Clear();
            _singleImageSubtreeCache.Clear();
        }

        private void BuildSingleImageCaches(Node root)
        {
            ClearSingleImageCaches();

            if (ReferenceEquals(root, null))
                return;

            BuildSingleImageCachesRecursive(root);
        }

        private bool BuildSingleImageCachesRecursive(Node fobject)
        {
            if (fobject.Data == null)
            {
                bool nodeSubtreeResult = true;

                if (!fobject.Children.IsEmpty())
                {
                    foreach (Node child in fobject.Children)
                    {
                        if (!BuildSingleImageCachesRecursive(child))
                        {
                            nodeSubtreeResult = false;
                        }
                    }
                }

                return nodeSubtreeResult;
            }

            bool canBeInside = EvaluateCanBeInsideSingleImage(fobject, applyReasons: true, out _);
            _singleImageInsideCache[fobject.Id ?? string.Empty] = canBeInside;

            bool subtreeCanBeSingleImage = canBeInside;

            if (!fobject.Children.IsEmpty())
            {
                foreach (Node child in fobject.Children)
                {
                    if (!BuildSingleImageCachesRecursive(child))
                    {
                        subtreeCanBeSingleImage = false;
                    }
                }
            }

            _singleImageSubtreeCache[fobject.Id ?? string.Empty] = subtreeCanBeSingleImage;
            return subtreeCanBeSingleImage;
        }

        private int CountAllNodes(Node root)
        {
            int count = 1;
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                {
                    count += CountAllNodes(child);
                }
            }
            return count;
        }

        private async Task SetTagsAsync(
            Node root,
            TagAlgorithm tagAlgorithm,
            CancellationToken importToken)
        {
            _outstandingTasks = 0;
            _currentConcurrency = 0;
            _tcs = new TaskCompletionSource<bool>();

            Interlocked.Increment(ref _outstandingTasks);
            TryProcessNode(root, tagAlgorithm, importToken);

            await _tcs.Task;
        }

        private void TryProcessNode(Node node, TagAlgorithm tagAlgorithm, CancellationToken importToken)
        {
            if (TryIncrementConcurrency())
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        ProcessNode(node, tagAlgorithm, importToken);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _currentConcurrency);

                        if (Interlocked.Decrement(ref _outstandingTasks) == 0)
                        {
                            _tcs.TrySetResult(true);
                        }
                    }
                });
            }
            else
            {
                Task.Run(() =>
                {
                    try
                    {
                        ProcessNode(node, tagAlgorithm, importToken);
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref _outstandingTasks) == 0)
                        {
                            _tcs.TrySetResult(true);
                        }
                    }
                }, importToken);
            }
        }

        private void ProcessNode(Node parent, TagAlgorithm tagAlgorithm, CancellationToken importToken)
        {
            importToken.ThrowIfCancellationRequested();

            if (parent.ContainsTag(FcuTag.Frame))
            {
                parent.Data.Hierarchy = new List<FcuHierarchy>
                {
                    new FcuHierarchy
                    {
                        Index = -1,
                        Name = parent.Data.Names.ObjectName,
                        Guid = parent.Data.Names.UitkGuid
                    }
                };
            }

            Interlocked.Increment(ref _processedElementsAll);
            monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(monoBeh, ProgressBarCategory.Tagging, _processedElementsAll);

            if (parent.Children == null)
                return;

            if (parent.Children.Count > monoBeh.Config.ChildParsingLimit && tagAlgorithm != TagAlgorithm.Figma)
                return;

            for (int i = 0; i < parent.Children.Count; i++)
            {
                importToken.ThrowIfCancellationRequested();

                Node child = parent.Children[i];
                bool shouldProcess = false;

                switch (tagAlgorithm)
                {
                    case TagAlgorithm.Figma:
                        SetTagsByFigma(ref child, ref parent, i);
                        parent.Children[i] = child;
                        shouldProcess = !child.Children.IsEmpty();
                        break;
                    case TagAlgorithm.Smart:
                        SetSmartTags(ref child, ref parent);
                        shouldProcess = HasVisibleItems(child.Children);
                        break;
                    case TagAlgorithm.Ignore:
                        SetIgnoredObjects(ref child, ref parent);
                        shouldProcess = HasVisibleItems(child.Children);
                        break;
                }

                if (shouldProcess)
                {
                    Interlocked.Increment(ref _outstandingTasks);
                    TryProcessNode(child, tagAlgorithm, importToken);
                }
            }
        }

        private bool TryIncrementConcurrency()
        {
            while (true)
            {
                int current = _currentConcurrency;

                if (current >= MAX_THREAD_COUNT)
                    return false;

                if (Interlocked.CompareExchange(ref _currentConcurrency, current + 1, current) == current)
                    return true;
            }
        }

        private void SetTagsByFigma(ref Node child, ref Node parent, int index)
        {
            child.Data = new SyncData
            {
                Id = child.Id,
                ProjectId = monoBeh.Settings.MainSettings.ProjectId,
                ChildIndexes = new List<int>(),
                Parent = parent,
                Graphic = monoBeh.GraphicHelpers.GetGraphic(child)
            };

            if (child.Name.IsScrollContent())
            {
                child.Data.ForceContainer = true;
            }
            else if (child.Name.IsScrollViewport())
            {
                child.Data.ForceContainer = true;
            }

            monoBeh.NameSetter.SetNames(child);

            if (TryGetManualTags(child, out List<FcuTag> manualTags))
            {
                for (int i = 0; i < manualTags.Count; i++)
                {
                    child.AddTag(manualTags[i]);
                    child.SetReason(ReasonKey.PerTag_ManualFigmaTag, manualTags[i]);
                    FcuLogger.Debug($"GetManualTag {i} | {child.Name} | {manualTags[i]}", FcuDebugSettingsFlags.LogSetTag);
                }

                if (manualTags.Contains(FcuTag.Ignore))
                {
                    child.Data.IsEmpty = true;
                }
                else if (manualTags.Contains(FcuTag.Image))
                {
                    child.Data.ForceImage = true;
                }
                else if (manualTags.Contains(FcuTag.Container))
                {
                    child.Data.ForceContainer = true;
                }
            }

            child.Data.IsEmpty = IsEmpty(child);

            if (child.ContainsTag(FcuTag.Background))
            {
                child.AddTag(FcuTag.Image);
                child.SetReason(ReasonKey.PerTag_IsBackground, FcuTag.Image);
            }

            if (parent.ContainsTag(FcuTag.Page))
            {
                child.AddTag(FcuTag.Frame);
                child.SetReason(ReasonKey.PerTag_ParentIsPage, FcuTag.Frame);
            }
            else
            {
                child.SetReason(ReasonKey.PerTag_Skip_ParentNotPage, FcuTag.Frame);
            }

            if (child.Type == NodeType.INSTANCE)
            {

            }

            if (child.LayoutWrap == LayoutWrap.WRAP ||
                child.LayoutMode == LayoutMode.HORIZONTAL ||
                child.LayoutMode == LayoutMode.VERTICAL)
            {
                if (HasVisibleItems(child.Children))
                {
                    child.AddTag(FcuTag.AutoLayoutGroup);
                    child.SetReason(ReasonKey.PerTag_HasLayoutMode, FcuTag.AutoLayoutGroup);
                }
                else
                {
                    child.SetReason(ReasonKey.PerTag_Skip_ALG_NoVisibleChildren, FcuTag.AutoLayoutGroup);
                }
            }
            else
            {
                child.SetReason(ReasonKey.PerTag_Skip_NoLayoutMode, FcuTag.AutoLayoutGroup);
            }

            if (child.PreserveRatio.ToBoolNullFalse())
            {
                child.AddTag(FcuTag.AspectRatioFitter);
                child.SetReason(ReasonKey.PerTag_PreserveRatio, FcuTag.AspectRatioFitter);
            }
            else
            {
                child.SetReason(ReasonKey.PerTag_Skip_PreserveRatioOff, FcuTag.AspectRatioFitter);
            }

            if (child.IsAnyMask())
            {
                child.AddTag(FcuTag.Mask);
                child.SetReason(ReasonKey.PerTag_IsAnyMask, FcuTag.Mask);
            }
            else
            {
                child.SetReason(ReasonKey.PerTag_Skip_NotAMask, FcuTag.Mask);
            }

            if (child.Name.ToLower() == "button")
            {
                child.AddTag(FcuTag.Button);
                child.SetReason(ReasonKey.PerTag_NameIsButton, FcuTag.Button);
            }
            else
            {
                child.SetReason(ReasonKey.PerTag_Skip_NameNotButton, FcuTag.Button);
            }

            if (child.Type == NodeType.TEXT)
            {
                child.AddTag(FcuTag.Text);
                child.SetReason(ReasonKey.PerTag_NodeTypeIsText, FcuTag.Text);

                if (child.Style.IsDefault() == false)
                {
                    if (child.Style.TextAutoResize == TextAutoResize.WIDTH_AND_HEIGHT || child.Style.TextAutoResize == TextAutoResize.HEIGHT)
                    {


                        bool parentIsAutoLayout = parent.LayoutWrap == LayoutWrap.WRAP
                            || parent.LayoutMode == LayoutMode.HORIZONTAL
                            || parent.LayoutMode == LayoutMode.VERTICAL;

                        if (!parentIsAutoLayout)
                        {
                            child.AddTag(FcuTag.ContentSizeFitter);
                            child.SetReason(ReasonKey.PerTag_AutoResizeText, FcuTag.ContentSizeFitter);
                        }
                        else
                        {
                            child.SetReason(ReasonKey.PerTag_Skip_ParentIsAutoLayout, FcuTag.ContentSizeFitter);
                        }
                    }
                    else
                    {
                        child.SetReason(ReasonKey.PerTag_Skip_NoAutoResize, FcuTag.ContentSizeFitter);
                    }
                }
            }
            else if (child.Type == NodeType.VECTOR)
            {
                child.AddTag(FcuTag.Image);
                child.SetReason(ReasonKey.PerTag_IsVector, FcuTag.Image);
            }
            else
            {
                bool hasVisibleFills = HasVisibleItems(child.Fills);
                bool hasVisibleStrokes = HasVisibleItems(child.Strokes);

                if (hasVisibleFills || hasVisibleStrokes)
                {
                    child.AddTag(FcuTag.Image);

                    if (hasVisibleFills)
                        child.SetReason(ReasonKey.PerTag_HasFills, FcuTag.Image);

                    if (hasVisibleStrokes)
                        child.SetReason(ReasonKey.PerTag_HasStrokes, FcuTag.Image);
                }
                else if (child.Type != NodeType.TEXT)
                {
                    child.SetReason(ReasonKey.PerTag_Skip_NoFillsOrStrokes, FcuTag.Image);
                }
            }

            if (child.Effects.IsEmpty() == false)
            {
                bool hasShadowEffect = false;
                bool hasBlurEffect = false;

                foreach (Effect effect in child.Effects)
                {
                    if (effect.IsShadowType())
                        hasShadowEffect = true;

                    if (effect.Type == EffectType.BACKGROUND_BLUR)
                        hasBlurEffect = true;

                    if (hasShadowEffect && hasBlurEffect)
                        break;
                }

                if (monoBeh.IsUGUI() && monoBeh.UsingTrueShadow() && !monoBeh.UsingSpriteRenderer())
                {
                    if (hasShadowEffect)
                    {
                        child.AddTag(FcuTag.Shadow);
                        child.SetReason(ReasonKey.PerTag_HasShadowEffects, FcuTag.Shadow);
                    }
                }
                else if (monoBeh.IsUITK())
                {
                    if (hasShadowEffect)
                    {
                        //child.AddTag(FcuTag.Shadow);
                        child.SetReason(ReasonKey.PerTag_Skip_FrameworkNoShadow, FcuTag.Shadow);
                    }
                }
                else if (monoBeh.IsNova())
                {
                    if (hasShadowEffect)
                    {
                        child.AddTag(FcuTag.Shadow);
                        child.SetReason(ReasonKey.PerTag_HasShadowEffects, FcuTag.Shadow);
                    }
                }
                else
                {

                    if (hasShadowEffect)
                        child.SetReason(ReasonKey.PerTag_Skip_FrameworkNoShadow, FcuTag.Shadow);
                }

                if (monoBeh.IsNova())
                {
                    if (hasBlurEffect)
                    {
                        child.AddTag(FcuTag.Blur);
                        child.SetReason(ReasonKey.PerTag_HasBackgroundBlur, FcuTag.Blur);
                    }
                }
                else if (hasBlurEffect)
                {
                    child.SetReason(ReasonKey.PerTag_Skip_FrameworkNoBlur, FcuTag.Blur);
                }
            }
            else
            {
                child.SetReason(ReasonKey.PerTag_Skip_NoShadowEffects, FcuTag.Shadow);
                child.SetReason(ReasonKey.PerTag_Skip_NoBlurEffects, FcuTag.Blur);
            }

            child.Data.IsOverlappedByStroke = IsOverlappedByStroke(child);

            if (monoBeh.Settings.MainSettings.DrawLayoutGrids && !child.LayoutGrids.IsEmpty())
            {
                child.AddTag(FcuTag.LayoutGrid);
                child.SetReason(ReasonKey.PerTag_HasLayoutGrids, FcuTag.LayoutGrid);
            }
            else if (!monoBeh.Settings.MainSettings.DrawLayoutGrids && !child.LayoutGrids.IsEmpty())
            {
                child.SetReason(ReasonKey.PerTag_Skip_DrawLayoutGridsOff, FcuTag.LayoutGrid);
            }
            else if (child.LayoutGrids.IsEmpty())
            {
                child.SetReason(ReasonKey.PerTag_Skip_NoLayoutGrids, FcuTag.LayoutGrid);
            }

            if (child.Opacity.HasValue && child.Opacity != 1)
            {
                child.AddTag(FcuTag.CanvasGroup);
                child.SetReason(ReasonKey.PerTag_OpacityNotOne, FcuTag.CanvasGroup);
            }
            else
            {
                child.SetReason(ReasonKey.PerTag_Skip_OpacityIsOne, FcuTag.CanvasGroup);
            }

            child.Data.Hierarchy.AddRange(parent.Data.Hierarchy);

            int sceneIndex = GetNewIndex(parent, index);
            child.Data.Hierarchy.Add(new FcuHierarchy
            {
                Index = sceneIndex,
                Name = child.Data.Names.ObjectName,
                Guid = child.Data.Names.UitkGuid,
            });
        }

        private bool SetSmartTags(ref Node child, ref Node parent)
        {
            string methodPath = $"{nameof(SetSmartTags)}";

            if (Is9slice(ref child))
            {
                child.AddTag(FcuTag.Slice9);
                child.SetReason(ReasonKey.PerTag_Is9SliceStructure, FcuTag.Slice9);
                child.AddTag(FcuTag.Image);
                child.Data.ForceImage = true;
                return true;
            }

            if (IsAutoSlice9(ref child))
            {
                child.AddTag(FcuTag.AutoSlice9);
                child.SetReason(ReasonKey.PerTag_PassedAutoSlice9Checks, FcuTag.AutoSlice9);
                child.AddTag(FcuTag.Image);

                if (!monoBeh.GraphicHelpers.CanDrawWithUnityImage(child, child.Data.Graphic))
                {
                    child.Data.ForceImage = true;
                }

                return true;
            }

            bool isInputFieldTextComponents = (parent.Name.IsInputTextArea() || parent.ContainsTag(FcuTag.InputField) || parent.ContainsTag(FcuTag.PasswordField)) && child.ContainsTag(FcuTag.Text);


            if (child.Data.IsEmpty && !isInputFieldTextComponents)
            {
                child.SetReason(ReasonKey.Tag_IsEmpty);
                FcuLogger.Debug($"{methodPath} | {child.Data.Reasons} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                return true;
            }

            if (child.Data.ForceImage)
            {
                child.SetReason(ReasonKey.Tag_ForceImage);
                FcuLogger.Debug($"{methodPath} | {child.Data.Reasons} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                return true;
            }

            if (parent.IsDrawableSolidOverlayContainer())
            {
                child.Data.IsEmpty = true;
                child.SetReason(ReasonKey.Tag_DrawableSolidOverlay);
                FcuLogger.Debug($"{methodPath} | {child.Data.Reasons} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                return true;
            }

            if (child.IsRootSprite(parent))
            {
                child.AddTag(FcuTag.Image);
                child.SetReason(ReasonKey.PerTag_IsRootSprite, FcuTag.Image);
                child.Data.ForceImage = true;

                child.SetReason(ReasonKey.Tag_IsRootSprite);
                FcuLogger.Debug($"{methodPath} | {child.Data.Reasons} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                return true;
            }

            if (monoBeh.Settings.MainSettings.RawImport == false)
            {
                bool hasButtonTags = child.ContainsCustomButtonTags();

                if (hasButtonTags)
                {
                    if (child.ContainsTag(FcuTag.Image))
                    {
                        if ((child.IsDownloadableType() || child.IsGenerativeType()) == false)
                        {
                            if (monoBeh.Settings.ButtonSettings.TransitionType == ButtonTransitionType.SpriteSwapForAll)
                            {
                                child.Data.ForceImage = true;
                                child.SetReason(ReasonKey.PerTag_SpriteSwapForAll, FcuTag.Image);
                                child.SetReason(ReasonKey.Tag_SpriteSwapForAll);
                                return true;
                            }
                        }
                    }
                }

                bool hasIcon = ContainsIcon(child);
                bool singleImage = CanBeSingleImage(child);
                FcuLogger.Debug($"{methodPath} | singleImage: {singleImage} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                if (hasIcon)
                {
                    child.Data.ForceContainer = true;
                    child.AddTag(FcuTag.Container);
                    child.SetReason(ReasonKey.PerTag_ContainsIcon, FcuTag.Container);

                    child.SetReason(ReasonKey.Tag_ContainsIcon);
                    FcuLogger.Debug($"{methodPath} | {string.Join(", ", child.Data.Reasons)} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                }
                else if (child.IsDrawableSolidOverlayContainer())
                {
                    child.AddTag(FcuTag.Image);
                    child.SetReason(ReasonKey.PerTag_DrawableSolidOverlay, FcuTag.Image);

                    child.SetReason(ReasonKey.Tag_DrawableSolidOverlay);
                    FcuLogger.Debug($"{methodPath} | {string.Join(", ", child.Data.Reasons)} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                    return true;
                }
                else if (singleImage && hasButtonTags)
                {
                    child.Data.ForceImage = true;
                    child.AddTag(FcuTag.Image);
                    child.SetReason(ReasonKey.PerTag_ButtonTagsSingleImage, FcuTag.Image);
                    child.RemoveNotDownloadableTags();

                    child.SetReason(ReasonKey.Tag_ContainsCustomButtonTags);
                    FcuLogger.Debug($"{methodPath} | {string.Join(", ", child.Data.Reasons)} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                    return true;
                }
                else if (singleImage)
                {
                    child.Data.ForceImage = true;
                    child.AddTag(FcuTag.Image);
                    child.SetReason(ReasonKey.PerTag_IsSingleImage, FcuTag.Image);
                    child.RemoveNotDownloadableTags();

                    child.SetReason(ReasonKey.Tag_SingleImage);
                    FcuLogger.Debug($"{methodPath} | {string.Join(", ", child.Data.Reasons)} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                    return true;
                }
                else if (child.Type == NodeType.BOOLEAN_OPERATION)
                {
                    child.Data.ForceImage = true;
                    child.AddTag(FcuTag.Image);
                    child.SetReason(ReasonKey.PerTag_IsBooleanOperation, FcuTag.Image);

                    child.SetReason(ReasonKey.Tag_BooleanOperation);
                    return true;
                }
                else
                {
                    FcuLogger.Debug($"{methodPath} | else | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                }
            }

            if (HasVisibleItems(child.Children))
            {
                child.SetReason(ReasonKey.Tag_ChildrenNotEmpty);
                FcuLogger.Debug($"{methodPath} | {child.Data.Reasons} | {child.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                child.AddTag(FcuTag.Container);
                child.SetReason(ReasonKey.PerTag_HasVisibleChildren, FcuTag.Container);
            }
            else
            {
                child.SetReason(ReasonKey.PerTag_Skip_NoChildren, FcuTag.Container);
            }

            return false;
        }

        private bool IsOverlappedByStroke(Node fobject)
        {
            bool blockedByStroke = false;

            if (HasVisibleItems(fobject.Fills) && HasVisibleItems(fobject.Strokes) && !fobject.ContainsTag(FcuTag.Shadow))
            {
                if (fobject.IndividualStrokeWeights.IsDefault())
                {
                    float twoSides = fobject.StrokeWeight * 2;

                    if (twoSides >= fobject.Size.y)
                    {
                        blockedByStroke = true;
                    }
                    else if (twoSides >= fobject.Size.x)
                    {
                        blockedByStroke = true;
                    }
                }
                else
                {
                    float topBottomStrokes = fobject.IndividualStrokeWeights.Top + fobject.IndividualStrokeWeights.Bottom;
                    float leftRightStrokes = fobject.IndividualStrokeWeights.Left + fobject.IndividualStrokeWeights.Right;

                    if (topBottomStrokes >= fobject.Size.y)
                    {
                        blockedByStroke = true;
                    }
                    else if (leftRightStrokes >= fobject.Size.x)
                    {
                        blockedByStroke = true;
                    }
                }
            }

            return blockedByStroke;
        }

        private static bool HasVisibleItems<T>(IEnumerable<T> items) where T : IVisible
        {
            if (items.IsEmpty())
                return false;

            foreach (T item in items)
            {
                if (item.Visible.ToBoolNullTrue())
                    return true;
            }

            return false;
        }

        private int GetNewIndex(Node parent, int figmaIndex)
        {
            int count = 0;

            for (int i = 0; i < figmaIndex; i++)
            {
                Node child = parent.Children[i];

                if (child.Data == null)
                {
                    break;
                }

                if (!child.Data.IsEmpty)
                {
                    count++;
                }
            }

            return count;
        }

        private bool Is9slice(ref Node fobject)
        {
            if (fobject.Children.IsEmpty())
            {
                fobject.SetReason(ReasonKey.Slice9_NoChildren);
                return false;
            }

            if (fobject.Children.Count != 9)
            {
                fobject.SetReason(ReasonKey.Slice9_Not9Children);
                return false;
            }

            AnchorType child0 = fobject.Children[0].GetFigmaAnchor();
            AnchorType child1 = fobject.Children[1].GetFigmaAnchor();
            AnchorType child2 = fobject.Children[2].GetFigmaAnchor();
            AnchorType child3 = fobject.Children[3].GetFigmaAnchor();
            AnchorType child4 = fobject.Children[4].GetFigmaAnchor();
            AnchorType child5 = fobject.Children[5].GetFigmaAnchor();
            AnchorType child6 = fobject.Children[6].GetFigmaAnchor();
            AnchorType child7 = fobject.Children[7].GetFigmaAnchor();
            AnchorType child8 = fobject.Children[8].GetFigmaAnchor();

            if (child0 == AnchorType.TopLeft &&
                child1 == AnchorType.HorStretchTop &&
                child2 == AnchorType.TopRight &&
                child3 == AnchorType.VertStretchLeft &&
                child4 == AnchorType.StretchAll &&
                child5 == AnchorType.VertStretchRight &&
                child6 == AnchorType.BottomLeft &&
                child7 == AnchorType.HorStretchBottom &&
                child8 == AnchorType.BottomRight)
            {
                fobject.SetReason(ReasonKey.Slice9_Applied);
                return true;
            }

            fobject.SetReason(ReasonKey.Slice9_WrongAnchors);
            return false;
        }

        private bool IsAutoSlice9(ref Node fobject)
        {

            if (!fobject.ContainsRoundedCorners())
            {
                fobject.SetReason(ReasonKey.Auto9_NoRoundedCorners);
                return false;
            }


            if (!fobject.IsRectangle())
            {
                fobject.SetReason(ReasonKey.Auto9_NotRectangle);
                return false;
            }


            if (!fobject.HasOnlySolidVisibleFillAndStroke())
            {
                fobject.SetReason(ReasonKey.Auto9_NotSingleColor);
                return false;
            }


            if (fobject.Effects.IsEmpty() == false)
            {
                foreach (Effect e in fobject.Effects)
                {
                    if (!e.IsVisible())
                        continue;

                    switch (e.Type)
                    {
                        case EffectType.DROP_SHADOW:
                        case EffectType.INNER_SHADOW:

                            if (e.Offset.x != 0 || e.Offset.y != 0)
                            {
                                fobject.SetReason(ReasonKey.Auto9_HasIncompatibleEffects);
                                return false;
                            }
                            break;

                        case EffectType.LAYER_BLUR:
                        case EffectType.BACKGROUND_BLUR:

                            break;

                        default:
                            fobject.SetReason(ReasonKey.Auto9_HasIncompatibleEffects);
                            return false;
                    }
                }
            }


            if (!fobject.Children.IsEmpty())
            {
                fobject.SetReason(ReasonKey.Auto9_HasChildren);
                return false;
            }

            fobject.SetReason(ReasonKey.Auto9_Applied);
            return true;
        }

        private void SetIgnoredObjects(ref Node child, ref Node parent)
        {
            if (child.Data.IsEmpty)
            {
                child.SetFlagToAllChilds(x => x.Data.IsEmpty = true);
                return;
            }

            if (child.Data.ForceImage)
            {
                child.SetFlagToAllChilds(x => x.Data.IsEmpty = true);
                return;
            }
        }

        internal bool TryGetManualTags(Node fobject, out List<FcuTag> tags)
        {
            tags = new List<FcuTag>();

            if (fobject.Name.Contains(monoBeh.Config.RealTagSeparator) == false)
            {
                return false;
            }

            string[] tagParts = GetTagParts(fobject.Name);

            for (int i = 0; i < tagParts.Length; i++)
            {
                tagParts[i] = CleanManualTagPart(tagParts[i]);
            }

            foreach (FcuTag fcuTag in ManualTagCandidates)
            {
                string figmaTag = fcuTag.GetTagConfig().FigmaTag.ToLower();

                if (figmaTag.IsEmpty())
                    continue;

                foreach (var tagPart in tagParts)
                {
                    if (tagPart == figmaTag)
                    {
                        tags.Add(fcuTag);
                        FcuLogger.Debug($"FindManualTag | tagPart: {tagPart} | tag: {figmaTag}", FcuDebugSettingsFlags.LogSetTag);
                    }
                }
            }

            return tags.Count > 0;
        }

        private string[] GetTagParts(string name)
        {
            string tempName = name.ToLower().Replace(" ", "");

            string[] nameParts = tempName.Split(monoBeh.Config.RealTagSeparator);

            if (nameParts.Length > 0)
            {
                string tagPart = nameParts[0];
                string[] tagParts = tagPart.Split(',');
                return tagParts;
            }

            return new string[] { };
        }

        private string CleanManualTagPart(string tagPart)
        {
            if (tagPart.IsEmpty())
                return string.Empty;

            bool hasOnlyLetters = true;

            for (int i = 0; i < tagPart.Length; i++)
            {
                char c = tagPart[i];

                if (c < 'a' || c > 'z')
                {
                    hasOnlyLetters = false;
                    break;
                }
            }

            if (hasOnlyLetters)
                return tagPart;

            char[] chars = new char[tagPart.Length];
            int count = 0;

            for (int i = 0; i < tagPart.Length; i++)
            {
                char c = tagPart[i];

                if (c < 'a' || c > 'z')
                    continue;

                chars[count++] = c;
            }

            if (count == 0)
                return string.Empty;

            return count == tagPart.Length ? tagPart : new string(chars, 0, count);
        }

        private bool ContainsIcon(Node fobject)
        {
            if (fobject.Children.IsEmpty())
                return false;

            foreach (Node item in fobject.Children)
            {
                if (item.Name.ToLower().Contains("icon"))
                    return true;

                if (ContainsIcon(item))
                    return true;
            }

            return false;
        }

        private bool CanBeSingleImage(Node fobject)
        {
            if (fobject.Children.IsEmpty())
            {
                FcuLogger.Debug($"Reason: 1 - Object has no children. | {fobject.Data.NameHierarchy}", FcuDebugSettingsFlags.LogSetTag);
                return false;
            }

            if (!string.IsNullOrEmpty(fobject.Id) &&
                _singleImageSubtreeCache.TryGetValue(fobject.Id, out bool subtreeCanBeSingleImage))
            {
                return subtreeCanBeSingleImage;
            }

            int count = 0;
            CanBeSingleImageRecursive(fobject, fobject, ref count);
            return count == 0;
        }

        private void CanBeSingleImageRecursive(Node fobject, Node parent, ref int count)
        {
            if (CanBeInsideSingleImage(fobject, parent) == false)
            {
                count++;
                return;
            }

            if (fobject.Children.IsEmpty())
                return;

            foreach (Node child in fobject.Children)
                CanBeSingleImageRecursive(child, parent, ref count);
        }

        private bool CanBeInsideSingleImage(Node fobject, Node parent)
        {
            bool canBeInside = EvaluateCanBeInsideSingleImage(fobject, applyReasons: true, out List<string> blockers);

            void LogCanBeInside(string message)
            {
                FcuLogger.Debug(
                    $"CanBeInsideSingleImage: {message} | Parent: {parent.Data.NameHierarchy}, Current: {fobject.Data.NameHierarchy}",
                    FcuDebugSettingsFlags.LogSetTag);
            }

            if (fobject.Data.ForceContainer)
            {
                LogCanBeInside("ForceContainer is true.");
                return false;
            }

            if (fobject.Data.ForceImage)
            {
                LogCanBeInside("ForceImage is true.");
                return false;
            }

            if (!canBeInside)
            {
                LogCanBeInside($"Tags {string.Join(", ", blockers)} cannot be inside a single image.");
                return false;
            }

            LogCanBeInside("All tags allow being inside single image.");
            return true;
        }

        internal static bool EvaluateCanBeInsideSingleImage(Node fobject, bool applyReasons, out List<string> blockers)
        {
            blockers = new List<string>();

            if (fobject.Data == null)
                return false;

            if (fobject.Data.ForceContainer)
            {
                if (applyReasons)
                {
                    fobject.SetReason(ReasonKey.Tag_ForceContainerTrue);
                }

                return false;
            }

            if (fobject.Data.ForceImage)
            {
                if (applyReasons)
                {
                    fobject.SetReason(ReasonKey.Tag_ForceImageTrue);
                }

                return false;
            }






            if (fobject.Type == NodeType.TEXT && !fobject.IsVisible())
            {
                if (applyReasons)
                {
                    fobject.SetReason(ReasonKey.Tag_TagCannotBeInsideSingleImage, new List<string> { "Text" });
                }

                return false;
            }

            if (fobject.Data.Tags.IsEmpty())
            {
                if (applyReasons)
                {
                    fobject.SetReason(ReasonKey.Tag_AllTagsAllowSingleImage);
                }

                return true;
            }

            foreach (FcuTag fcuTag in fobject.Data.Tags)
            {
                TagConfig tc = fcuTag.GetTagConfig();

                if (tc.CanBeInsideSingleImage == false)
                    blockers.Add(fcuTag.ToString());
            }

            if (blockers.Count > 0)
            {
                if (applyReasons)
                {
                    fobject.SetReason(ReasonKey.Tag_TagCannotBeInsideSingleImage, blockers);
                }

                return false;
            }

            if (applyReasons)
            {
                fobject.SetReason(ReasonKey.Tag_AllTagsAllowSingleImage);
            }

            return true;
        }


        private bool IsEmpty(Node fobject)
        {
            int count = 0;
            IsEmptyRecursive(fobject, ref count);
            return count == 0;
        }

        private void IsEmptyRecursive(Node fobject, ref int count)
        {
            if (count > 0)
                return;

            if (!fobject.IsVisible())
                return;

            if (fobject.ContainsTag(FcuTag.Ignore))
                return;

            if (fobject.IsZeroSize() && fobject.Type != NodeType.LINE)
                return;

            bool hasFills = HasVisibleItems(fobject.Fills);
            bool hasStrokes = HasVisibleItems(fobject.Strokes);
            bool hasEffects = HasVisibleItems(fobject.Effects);

            if (hasFills || hasStrokes || hasEffects || fobject.IsObjectMask())
            {
                count++;
                return;
            }

            if (!HasVisibleItems(fobject.Children))
                return;

            foreach (Node item in fobject.Children)
                IsEmptyRecursive(item, ref count);
        }

        public void CountTags(List<Node> fobjects)
        {
            Dictionary<FcuTag, int> tagsCounter = new Dictionary<FcuTag, int>(AllTags.Length);

            foreach (FcuTag tag in AllTags)
            {
                tagsCounter[tag] = 0;
            }

            foreach (Node fobject in fobjects)
            {
                if (fobject.Data.GameObject == null)
                {
                    continue;
                }

                foreach (FcuTag tag in fobject.Data.Tags)
                {
                    tagsCounter[tag]++;
                }
            }

            this.TagsCounter = tagsCounter;
        }

        enum TagAlgorithm
        {
            Figma,
            Smart,
            Ignore
        }
    }
}
#endif