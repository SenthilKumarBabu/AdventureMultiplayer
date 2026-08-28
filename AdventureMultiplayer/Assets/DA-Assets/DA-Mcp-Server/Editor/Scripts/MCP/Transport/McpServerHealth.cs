using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;

namespace DA_Assets.Shared.MCP
{
    internal static class McpServerHealth
    {
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(0.75)
        };

        private static bool _pollInFlight;
        private static double _nextPollTime;

        public static bool IsReachable { get; private set; }
        public static string LastMessage { get; private set; } = "Not checked";

        public static event Action Changed;

        public static void PollIfDue(double intervalSeconds = 0.75)
        {
            if (_pollInFlight || EditorApplication.timeSinceStartup < _nextPollTime)
            {
                return;
            }

            _nextPollTime = EditorApplication.timeSinceStartup + intervalSeconds;
            _ = PollNowAsync();
        }

        public static async Task<bool> PollNowAsync()
        {
            if (_pollInFlight)
            {
                return IsReachable;
            }

            _pollInFlight = true;
            bool previousReachable = IsReachable;
            string previousMessage = LastMessage;

            try
            {
                HttpResponseMessage response = await Client.GetAsync(McpServerProcess.HealthUrl).ConfigureAwait(false);
                IsReachable = response.IsSuccessStatusCode;
                LastMessage = IsReachable ? "HTTP health endpoint is reachable" : $"HTTP {(int)response.StatusCode}";
            }
            catch (Exception ex)
            {
                IsReachable = false;
                LastMessage = ex.Message;
            }
            finally
            {
                _pollInFlight = false;
            }

            if (previousReachable != IsReachable || previousMessage != LastMessage)
            {
                Changed?.Invoke();
            }

            return IsReachable;
        }
    }
}
