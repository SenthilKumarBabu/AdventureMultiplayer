#if UNITY_EDITOR
using System;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public struct ZipManifest
    {
        public float ExportScale;
        public string ImageFormat;
        public bool IsValid => ExportScale > 0f && !string.IsNullOrWhiteSpace(ImageFormat);
    }
}
#endif