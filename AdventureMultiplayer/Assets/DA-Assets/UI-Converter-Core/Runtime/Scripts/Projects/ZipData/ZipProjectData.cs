#if UNITY_EDITOR
using System;
using System.IO;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public struct ZipProjectData
    {
        public string ExtractedFolderPath;
        public string ManifestFileName;

        public string ArchiveHash;

        public string JsonFilePath => Path.Combine(ExtractedFolderPath, "project.json");
        public string NodesFolder => Path.Combine(ExtractedFolderPath, "nodes");
        public string ImagesFolder => Path.Combine(ExtractedFolderPath, "images");
        public string ManifestFilePath => Path.Combine(ExtractedFolderPath, ManifestFileName);

        public bool IsValid => !string.IsNullOrEmpty(ExtractedFolderPath)
                            && File.Exists(JsonFilePath);

        public ZipProjectData(string extractedFolderPath, string manifestFileName)
        {
            ExtractedFolderPath = extractedFolderPath;
            ManifestFileName = manifestFileName;
            ArchiveHash = null;
        }

        public static string SanitizeNodeId(string id) => id.Replace(":", "_").Replace("/", "_");
    }
}
#endif