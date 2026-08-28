using UnityEditor;

namespace DA_Assets.Shared.MCP
{
    internal static class McpServerDesiredState
    {
        private const string DesiredRunningPrefix = "DA_Assets.MCP.LocalHttpServer.DesiredRunning.";

        public static bool IsRunningDesired => EditorPrefs.GetBool(Key, false);

        public static void SetRunningDesired(bool value)
        {
            EditorPrefs.SetBool(Key, value);
        }

        private static string Key => DesiredRunningPrefix + ProjectIdentity.ProjectHash;
    }
}
