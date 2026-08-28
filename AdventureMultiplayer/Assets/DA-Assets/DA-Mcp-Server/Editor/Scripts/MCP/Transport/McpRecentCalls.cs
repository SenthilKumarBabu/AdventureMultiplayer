using System;
using System.Collections.Generic;
using System.Linq;

namespace DA_Assets.Shared.MCP
{
    internal readonly struct McpRecentCall
    {
        public McpRecentCall(DateTime time, string toolName, bool success, string error)
        {
            Time = time;
            ToolName = toolName;
            Success = success;
            Error = error;
        }

        public DateTime Time { get; }
        public string ToolName { get; }
        public bool Success { get; }
        public string Error { get; }
    }

    internal static class McpRecentCalls
    {
        private const int Capacity = 200;
        private static readonly Queue<McpRecentCall> Calls = new();

        public static IReadOnlyList<McpRecentCall> List()
        {
            return Calls.Reverse().ToList();
        }

        public static void Add(string toolName, bool success, string error)
        {
            Calls.Enqueue(new McpRecentCall(DateTime.Now, toolName, success, error));
            while (Calls.Count > Capacity)
            {
                Calls.Dequeue();
            }
        }
    }
}
