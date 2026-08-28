using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    internal static class McpTransportAutoRestore
    {
        private static bool _restoreInFlight;

        [InitializeOnLoadMethod]
        private static void Restore()
        {
            EditorApplication.delayCall += () => _ = RestoreAsync();
        }

        private static async Task RestoreAsync()
        {
            if (_restoreInFlight || !McpServerDesiredState.IsRunningDesired)
            {
                return;
            }

            _restoreInFlight = true;

            try
            {
                McpServerProcess.Start();
                await TransportManager.StartWhenReachableAsync(McpServerProcess.HttpUrl);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DA MCP server auto-restore failed: {ex.Message}");
            }
            finally
            {
                _restoreInFlight = false;
            }
        }
    }
}
