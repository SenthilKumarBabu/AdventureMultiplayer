using System.Collections.Generic;
using System.IO;

namespace DA_Assets.Shared.MCP
{
    public enum McpClientStatus
    {
        NotInstalled,
        NotConfigured,
        Configured,
        Error
    }

    public abstract class McpClientConfigurator
    {
        protected McpClientConfigurator(string id, string displayName, string configPath)
        {
            Id = id;
            DisplayName = displayName;
            ConfigPath = configPath;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string ConfigPath { get; }
        public string LastError { get; protected set; }
        public virtual bool SupportsAutoConfigure => true;
        public virtual bool IsInstalled => ParentDirectoryExists(ConfigPath);

        public abstract McpClientStatus CheckStatus();
        public abstract void Configure(IReadOnlyList<McpServerConfig> configs);
        public abstract string GetManualSnippet(IReadOnlyList<McpServerConfig> configs);

        protected static bool ParentDirectoryExists(string path)
        {
            string parent = Path.GetDirectoryName(path);
            return !string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent);
        }
    }
}
