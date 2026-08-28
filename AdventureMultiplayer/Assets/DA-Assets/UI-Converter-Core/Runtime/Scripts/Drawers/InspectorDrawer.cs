#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DA_Assets.UCC.Drawers
{
    [Serializable]
    public class InspectorDrawer : FcuBase
    {
        public Action OnFramesChanged { get; set; }
        public Action OnScrollContentUpdated { get; set; }


        public SelectableNode FillSelectableFramesArray(Node document, int maxDepth = 2)
        {
            SelectableNode doc = new SelectableNode();

            FillNewSelectableItemRecursively(doc, document, 0, maxDepth);

            bool same = CompareIdsRecursively(_document, doc);

            if (!same)
            {
                _document = doc;
                _document.SetAllSelected(true);
            }

            this.OnScrollContentUpdated?.Invoke();
            this.OnFramesChanged?.Invoke();

            return _document;
        }


        public void FillNewSelectableItemRecursively(SelectableNode parentItem, Node parent, int currentDepth, int maxDepth)
        {
            parentItem.Id = parent.Id;
            parentItem.Type = parent.Type;
            parentItem.Name = parent.Name;

            if (currentDepth > maxDepth)
                return;

            if (parent.Children.IsEmpty())
                return;

            foreach (Node child in parent.Children)
            {
                bool isAllowed = IsAllowed(child, parent);

                if (!isAllowed)
                    continue;

                SelectableNode childItem = new SelectableNode();
                FillNewSelectableItemRecursively(childItem, child, currentDepth + 1, maxDepth);
                parentItem.Childs.Add(childItem);
            }
        }

        private bool IsAllowed(Node fobject, Node parent)
        {
            monoBeh.TagSetter.TryGetManualTags(fobject, out List<FcuTag> manualTags);

            if (manualTags.Contains(FcuTag.Ignore))
            {
                return false;
            }

            if (!fobject.IsVisible())
            {
                return false;
            }

            if (fobject.Type == NodeType.CANVAS)
            {
                return true;
            }

            if (parent.Type == NodeType.CANVAS)
            {
                return true;
            }

            return false;
        }

        private bool CompareIdsRecursively(SelectableNode item1, SelectableNode item2)
        {
            if (item1.Id != item2.Id)
                return false;

            if (item1.Childs.Count != item2.Childs.Count)
                return false;

            for (int i = 0; i < item1.Childs.Count; i++)
            {
                if (!CompareIdsRecursively(item1.Childs[i], item2.Childs[i]))
                    return false;
            }

            return true;
        }

        [SerializeField] SelectableNode _document = new SelectableNode();
        public SelectableNode SelectableDocument => _document;
    }
}
#endif