using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace DA_Assets.Shared.MCP
{
    [InitializeOnLoad]
    public static class TransportManager
    {
        private const string HttpBaseUrlKey = "DA_Assets.MCP.HttpBaseUrl";
        private const string AutoConnectKey = "DA_Assets.MCP.AutoConnect";
        private static readonly CommandRegistry Registry = new();
        private static readonly WebSocketTransportClient WebSocketClient = new(Registry);
        private static readonly SemaphoreSlim ConnectionGate = new(1, 1);

        static TransportManager()
        {
        }

        public static bool IsHttpConnected => WebSocketClient.IsConnected;
        public static bool AutoConnectEnabled => EditorPrefs.GetBool(AutoConnectKey, true);
        public static string SessionId => WebSocketClient.SessionId;
        public static IReadOnlyCollection<ToolDescriptor> ToolDescriptors => Registry.ToolDescriptors;
        public static IReadOnlyCollection<ResourceDescriptor> ResourceDescriptors => Registry.ResourceDescriptors;

        public static Task StartHttpAsync(string baseUrl)
        {
            EditorPrefs.SetBool(AutoConnectKey, true);

            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                EditorPrefs.SetString(HttpBaseUrlKey, baseUrl);
            }

            return StartHttpLockedAsync(GetHttpBaseUrl());
        }

        public static async Task StartWhenReachableAsync(string baseUrl, double timeoutSeconds = 8.0)
        {
            EditorPrefs.SetBool(AutoConnectKey, true);

            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                EditorPrefs.SetString(HttpBaseUrlKey, baseUrl);
            }

            DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                if (await McpServerHealth.PollNowAsync())
                {
                    await StartHttpAsync(GetHttpBaseUrl());
                    return;
                }

                await Task.Delay(250);
            }

            await StartHttpAsync(GetHttpBaseUrl());
        }

        public static Task StopHttpAsync()
        {
            EditorPrefs.SetBool(AutoConnectKey, false);
            return StopHttpLockedAsync();
        }

        public static string GetHttpBaseUrl()
        {
            return EditorPrefs.GetString(HttpBaseUrlKey, "http://127.0.0.1:8765");
        }

        public static async Task TryRestoreHttpSessionAsync()
        {
            try
            {
                await StartHttpAsync(GetHttpBaseUrl());
            }
            catch
            {
            }
        }

        private static void StartHttpFromMenu()
        {
            _ = StartHttpAsync(GetHttpBaseUrl());
        }

        private static void StopHttpFromMenu()
        {
            _ = StopHttpAsync();
        }

        private static async Task StartHttpLockedAsync(string baseUrl)
        {
            await ConnectionGate.WaitAsync();
            try
            {
                if (WebSocketClient.IsActive)
                {
                    return;
                }

                await WebSocketClient.StartAsync(baseUrl);
            }
            finally
            {
                ConnectionGate.Release();
            }
        }

        private static async Task StopHttpLockedAsync()
        {
            await ConnectionGate.WaitAsync();
            try
            {
                await WebSocketClient.StopAsync();
            }
            finally
            {
                ConnectionGate.Release();
            }
        }

        [MenuItem("Tools/D.A. Assets/MCP Control", priority = -100)]
        private static void OpenControlWindow()
        {
            McpControlWindow.ShowWindow();
        }
    }
}
