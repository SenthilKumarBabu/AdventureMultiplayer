#if UNITY_EDITOR
using System;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class FlexibleImageSettings : BaseImageSettings
    {
        public FlexibleImageFeatherMode FeatherMode { get => featherMode; set => featherMode = value; }
        public float Softness { get => softness; set => softness = value; }
        public int MeshSubdivisions { get => meshSubdivisions; set => meshSubdivisions = value; }

        [Serializable]
        public enum FlexibleImageFeatherMode
        {
            Inwards,
            Outwards,
            Bidirectional
        }

        [UnityEngine.SerializeField] private FlexibleImageFeatherMode featherMode = FlexibleImageFeatherMode.Bidirectional;
        [UnityEngine.SerializeField] private float softness = 1f;
        [UnityEngine.SerializeField] private int meshSubdivisions = 3;
    }
}
#endif