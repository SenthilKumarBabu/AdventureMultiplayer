#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class AutoLayoutDrawer : FcuBase
    {
        public override void Init(ConverterBase monoBeh)
        {
            base.Init(monoBeh);

            GridLayoutDrawer.Init(monoBeh);
            VertLayoutDrawer.Init(monoBeh);
            HorLayoutDrawer.Init(monoBeh);
            LayoutElementDrawer.Init(monoBeh);
        }

        public void Draw(Node fobject)
        {
            if (fobject.Data.FRect.absoluteAngle != 0)
            {
                return;
            }

            foreach (int index in fobject.Data.ChildIndexes)
            {
                if (monoBeh.CurrentProject.TryGetByIndex(index, out Node child))
                {
                    if (child.Data.FRect.absoluteAngle != 0)
                    {
                        return;
                    }

                    if (child.Data.GameObject != null)
                    {
                        this.LayoutElementDrawer.Draw(child, fobject);
                    }
                }
            }

            if (fobject.Data.GameObject.TryGetComponentSafe(out LayoutGroup oldLayoutGroup))
            {
                oldLayoutGroup.Destroy();
            }

            if (fobject.LayoutWrap == LayoutWrap.WRAP)
            {
                this.GridLayoutDrawer.Draw(fobject);
            }
            else if (fobject.LayoutMode == LayoutMode.HORIZONTAL)
            {
                this.HorLayoutDrawer.Draw(fobject);
            }
            else if (fobject.LayoutMode == LayoutMode.VERTICAL)
            {
                this.VertLayoutDrawer.Draw(fobject);
            }


            if (fobject.PrimaryAxisAlignItems == PrimaryAxisAlignItem.SPACE_BETWEEN)
            {
                InsertSpaceBetweenSpacers(fobject);
            }
        }

        private void InsertSpaceBetweenSpacers(Node fobject)
        {
            Transform parentTransform = fobject.Data.GameObject.transform;


            List<Transform> activeChildren = new List<Transform>();
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform child = parentTransform.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    activeChildren.Add(child);
                }
            }

            if (activeChildren.Count < 2)
                return;

            bool isHorizontal = fobject.LayoutMode == LayoutMode.HORIZONTAL;
            GameObject prefab = isHorizontal
                ? monoBeh.Config.HorizontalSpacePrefab
                : monoBeh.Config.VerticalSpacePrefab;

            if (prefab == null)
                return;



            for (int i = activeChildren.Count - 1; i > 0; i--)
            {
                GameObject spacer = UnityEngine.Object.Instantiate(prefab, parentTransform);
                spacer.name = prefab.name + "-" + (i - 1);


                int siblingIndex = activeChildren[i].GetSiblingIndex();
                spacer.transform.SetSiblingIndex(siblingIndex);
            }
        }

        [SerializeField] public GridLayoutDrawer GridLayoutDrawer = new GridLayoutDrawer();
        [SerializeField] public VertLayoutDrawer VertLayoutDrawer = new VertLayoutDrawer();
        [SerializeField] public HorLayoutDrawer HorLayoutDrawer = new HorLayoutDrawer();
        [SerializeField] public LayoutElementDrawer LayoutElementDrawer = new LayoutElementDrawer();
    }
}
#endif