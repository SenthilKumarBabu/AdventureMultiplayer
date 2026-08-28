using System.IO;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    internal static class ProjectIdentity
    {
        public static string ProjectName
        {
            get
            {
                string projectRoot = ProjectPath;
                return string.IsNullOrEmpty(projectRoot) ? "Unknown Project" : Path.GetFileName(projectRoot);
            }
        }

        public static string ProjectHash => Hash128.Compute(Application.dataPath).ToString();

        public static string ProjectPath
        {
            get
            {
                string dataPath = Application.dataPath?.TrimEnd('/', '\\');
                if (string.IsNullOrEmpty(dataPath))
                {
                    return string.Empty;
                }

                return string.Equals(Path.GetFileName(dataPath), "Assets")
                    ? Path.GetDirectoryName(dataPath)
                    : dataPath;
            }
        }
    }
}
