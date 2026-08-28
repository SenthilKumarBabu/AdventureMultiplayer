using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    [InitializeOnLoad]
    internal static class McpBridgeAutoConnector
    {
        private static bool _connectInFlight;
        private static double _nextConnectAttemptTime;

        static McpBridgeAutoConnector()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            McpServerHealth.PollIfDue();

            if (!McpServerHealth.IsReachable || TransportManager.IsHttpConnected || _connectInFlight)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextConnectAttemptTime)
            {
                return;
            }

            _nextConnectAttemptTime = EditorApplication.timeSinceStartup + 2.0;
            _connectInFlight = true;
            _ = ConnectAsync();
        }

        private static async Task ConnectAsync()
        {
            try
            {
                await TransportManager.StartHttpAsync(McpServerProcess.HttpUrl);
            }
            catch (Exception ex)
            {
                //Debug.LogWarning($"DA MCP bridge auto-connect failed: {ex.Message}");
            }
            finally
            {
                _connectInFlight = false;
            }
        }
    }
}
