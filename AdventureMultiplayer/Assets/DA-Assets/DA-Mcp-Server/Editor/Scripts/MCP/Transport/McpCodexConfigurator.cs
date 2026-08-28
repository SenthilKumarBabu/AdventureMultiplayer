using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DA_Assets.Shared.MCP.External.Tommy;

namespace DA_Assets.Shared.MCP
{
    public sealed class McpCodexConfigurator : McpClientConfigurator
    {
        public McpCodexConfigurator(string configPath)
            : base("codex", "Codex", configPath)
        {
        }

        public override McpClientStatus CheckStatus()
        {
            LastError = null;
            if (!IsInstalled)
            {
                return McpClientStatus.NotInstalled;
            }

            if (!File.Exists(ConfigPath))
            {
                return McpClientStatus.NotConfigured;
            }

            try
            {
                TomlTable root = TryParseToml(File.ReadAllText(ConfigPath));
                if (root == null || !TryGetTable(root, "mcp_servers", out TomlTable servers))
                {
                    return McpClientStatus.NotConfigured;
                }

                bool hasAny = servers.RawTable.Values
                    .OfType<TomlTable>()
                    .Any(entry => IsConfiguredDaServer(entry));

                return hasAny ? McpClientStatus.Configured : McpClientStatus.NotConfigured;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return McpClientStatus.Error;
            }
        }

        public override void Configure(IReadOnlyList<McpServerConfig> configs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            string existing = File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath) : string.Empty;
            TomlTable root = ParseExistingConfig(existing);

            if (!root.TryGetNode("mcp_servers", out TomlNode mcpServersNode) || mcpServersNode is not TomlTable mcpServers)
            {
                mcpServers = new TomlTable();
                root["mcp_servers"] = mcpServers;
            }

            foreach (McpServerConfig config in configs)
            {
                if (config == null || string.IsNullOrEmpty(config.DisplayName) || string.IsNullOrEmpty(config.EndpointPath))
                {
                    continue;
                }

                mcpServers[config.DisplayName] = BuildHttpEntry(config);
            }

            string updated;
            using (var writer = new StringWriter())
            {
                root.WriteTo(writer);
                updated = writer.ToString();
            }

            string tmpPath = ConfigPath + ".tmp";
            File.WriteAllText(tmpPath, updated);
            if (File.Exists(ConfigPath))
            {
                File.Replace(tmpPath, ConfigPath, ConfigPath + ".bak");
            }
            else
            {
                File.Move(tmpPath, ConfigPath);
            }
        }

        public override string GetManualSnippet(IReadOnlyList<McpServerConfig> configs)
        {
            var root = new TomlTable();
            var mcpServers = new TomlTable();

            foreach (McpServerConfig config in configs)
            {
                if (config == null || string.IsNullOrEmpty(config.DisplayName) || string.IsNullOrEmpty(config.EndpointPath))
                {
                    continue;
                }

                mcpServers[config.DisplayName] = BuildHttpEntry(config);
            }

            root["mcp_servers"] = mcpServers;
            using var writer = new StringWriter();
            root.WriteTo(writer);
            return writer.ToString();
        }

        private static TomlTable ParseExistingConfig(string toml)
        {
            if (string.IsNullOrWhiteSpace(toml))
            {
                return new TomlTable();
            }

            TomlTable root = TryParseToml(toml);
            if (root == null)
            {
                throw new InvalidOperationException("Codex config TOML is invalid. Refusing to overwrite it.");
            }

            return root;
        }

        private static TomlTable TryParseToml(string toml)
        {
            if (string.IsNullOrWhiteSpace(toml))
            {
                return null;
            }

            try
            {
                using var reader = new StringReader(toml);
                return TOML.Parse(reader);
            }
            catch (TomlParseException)
            {
                return null;
            }
            catch (TomlSyntaxException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static TomlTable BuildHttpEntry(McpServerConfig config)
        {
            string url = $"{McpServerProcess.HttpUrl}/{config.EndpointPath}/mcp";
            return new TomlTable
            {
                ["type"] = new TomlString { Value = "http" },
                ["url"] = new TomlString { Value = url },
                ["disabled"] = new TomlBoolean { Value = false }
            };
        }

        private static bool IsConfiguredDaServer(TomlTable entry)
        {
            string url = GetTomlString(entry, "url");
            return url != null
                && url.Contains(McpServerProcess.Host, StringComparison.OrdinalIgnoreCase)
                && url.Contains(McpServerProcess.Port.ToString(), StringComparison.Ordinal);
        }

        private static bool TryGetTable(TomlTable parent, string key, out TomlTable table)
        {
            table = null;
            if (parent == null)
            {
                return false;
            }

            if (parent.TryGetNode(key, out TomlNode node))
            {
                if (node is TomlTable tbl)
                {
                    table = tbl;
                    return true;
                }

                if (node is TomlArray array)
                {
                    TomlTable firstTable = array.Children.OfType<TomlTable>().FirstOrDefault();
                    if (firstTable != null)
                    {
                        table = firstTable;
                        return true;
                    }
                }
            }

            return false;
        }

        private static string GetTomlString(TomlTable table, string key)
        {
            if (table != null && table.TryGetNode(key, out TomlNode node))
            {
                if (node is TomlString str)
                {
                    return str.Value;
                }

                if (node.HasValue)
                {
                    return node.ToString();
                }
            }

            return null;
        }
    }
}
