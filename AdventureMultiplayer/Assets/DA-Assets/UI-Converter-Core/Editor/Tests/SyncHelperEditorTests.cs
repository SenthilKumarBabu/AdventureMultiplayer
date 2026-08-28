#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.UCC.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC.Tests.Editor
{
    public sealed class SyncHelperEditorTests
    {
        [Test]
        public void BuildInfoCard_LongHierarchyAndTags_SeparatesWrappedTextFromWrappedTags()
        {
            GameObject gameObject = new GameObject(nameof(SyncHelperEditorTests));
            try
            {
                SyncHelper syncHelper = gameObject.AddComponent<SyncHelper>();
                syncHelper.Data = new SyncData
                {
                    Hierarchy = new List<FcuHierarchy>
                    {
                        new FcuHierarchy { Name = "RoundEndPopup" },
                        new FcuHierarchy { Name = "SimplifiedProfilePanel" }
                    },
                    Tags = new List<FcuTag>
                    {
                        FcuTag.Frame,
                        FcuTag.AutoLayoutGroup,
                        FcuTag.AspectRatioFitter,
                        FcuTag.Mask
                    }
                };

                Type editorType = Type.GetType("DA_Assets.UCC.SyncHelperEditor, DA_Assets.UCC.Editor");
                UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(syncHelper, editorType);
                try
                {
                    DAInspectorUITK uitk = Resources.Load<DAInspectorUITK>("DAInspectorUITK");
                    Assert.That(uitk, Is.Not.Null);

                    editorType.GetField("_uitk", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(editor, uitk);

                    object[] args = { new Toggle("Debug"), new VisualElement(), null };
                    VisualElement card = (VisualElement)editorType
                        .GetMethod("BuildInfoCard", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(editor, args);

                    VisualElement hierarchyRow = card.Q<VisualElement>("SyncHelperHierarchyRow");
                    Label hierarchyLabel = hierarchyRow.Q<Label>("SyncHelperHierarchyLabel");
                    VisualElement tagsRow = card.Q<VisualElement>("SyncHelperTagsRow");

                    Assert.That(hierarchyRow.style.flexDirection.value, Is.EqualTo(FlexDirection.Row));
                    Assert.That(hierarchyLabel.style.whiteSpace.value, Is.EqualTo(WhiteSpace.Normal));
                    Assert.That(hierarchyLabel.style.flexGrow.value, Is.EqualTo(1f));
                    Assert.That(tagsRow.style.flexWrap.value, Is.EqualTo(Wrap.Wrap));
                    Assert.That(tagsRow.childCount, Is.EqualTo(4));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
#endif