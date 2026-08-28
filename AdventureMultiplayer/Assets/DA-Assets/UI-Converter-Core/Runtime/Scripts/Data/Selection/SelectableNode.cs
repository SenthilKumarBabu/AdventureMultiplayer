#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class SelectableNode
    {
        [SerializeField] string id;
        public string Id { get => id; set => id = value; }

        [SerializeField] string name;
        public string Name { get => name; set => name = value; }

        [SerializeField] NodeType type;
        public NodeType Type { get => type; set => type = value; }

        [SerializeField] bool selected;
        public bool Selected { get => selected; set => selected = value; }

        [SerializeField, SerializeReference] List<SelectableNode> childs = new List<SelectableNode>();
        public List<SelectableNode> Childs { get => childs; set => childs = value; }

        public void SetAllSelected(bool value)
        {
            selected = value;

            foreach (SelectableNode child in childs)
            {
                child.SetAllSelected(value);
            }
        }
    }
}
#endif