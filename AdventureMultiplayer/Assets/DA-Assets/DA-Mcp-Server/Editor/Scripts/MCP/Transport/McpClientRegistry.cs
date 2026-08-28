using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    internal static class McpClientRegistry
    {
        public static IReadOnlyList<McpClientConfigurator> All()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return new McpClientConfigurator[]
            {
                Json("antigravity", "Antigravity", Path.Combine(home, ".gemini", "antigravity", "mcp_config.json"), "serverUrl"),
                Json("claudecode", "Claude Code", Path.Combine(home, ".claude", "mcp.json")),
                Json("claudedesktop", "Claude Desktop", Path.Combine(appData, "Claude", "claude_desktop_config.json")),
                new McpCodexConfigurator(Path.Combine(home, ".codex", "config.toml")),
                Json("cursor", "Cursor", Path.Combine(home, ".cursor", "mcp.json")),
                Json("geminicli", "Gemini CLI", Path.Combine(home, ".gemini", "settings.json"), "httpUrl"),
                Json("windsurf", "Windsurf", Path.Combine(home, ".codeium", "windsurf", "mcp_config.json"), "serverUrl")
            };
        }

        private static McpJsonConfigurator Json(string id, string name, string path, string urlProperty = "url", bool vsCodeLayout = false)
        {
            return new McpJsonConfigurator(id, name, path, urlProperty, vsCodeLayout);
        }
    }
}
