using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if JSONNET_EXISTS
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace DA_Assets.Shared.MCP
{
    public sealed class McpJsonConfigurator : McpClientConfigurator
    {
        private readonly string _urlProperty;
        private readonly bool _vsCodeLayout;

        public McpJsonConfigurator(string id, string displayName, string configPath, string urlProperty = "url", bool vsCodeLayout = false)
            : base(id, displayName, configPath)
        {
            _urlProperty = urlProperty;
            _vsCodeLayout = vsCodeLayout;
        }

        public override McpClientStatus CheckStatus()
        {
#if JSONNET_EXISTS
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
                JObject root = JObject.Parse(File.ReadAllText(ConfigPath));
                JObject container = GetServerContainer(root);
                if (container == null)
                {
                    return McpClientStatus.NotConfigured;
                }

                bool hasAny = container.Properties().Any(p =>
                {
                    JObject entry = p.Value as JObject;
                    if (entry == null) return false;
                    string url = (string)(entry["url"] ?? entry["serverUrl"] ?? entry["httpUrl"]);
                    return url != null && url.Contains(McpServerProcess.Host) && url.Contains(McpServerProcess.Port.ToString());
                });

                return hasAny ? McpClientStatus.Configured : McpClientStatus.NotConfigured;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return McpClientStatus.Error;
            }
#else
            LastError = "Newtonsoft.Json is required for JSON configurators.";
            return McpClientStatus.Error;
#endif
        }

        public override void Configure(IReadOnlyList<McpServerConfig> configs)
        {
#if JSONNET_EXISTS
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            JObject root = File.Exists(ConfigPath)
                ? JObject.Parse(File.ReadAllText(ConfigPath))
                : new JObject();

            JObject container = GetOrCreateServerContainer(root);

            container.Remove("daMCP");

            foreach (McpServerConfig config in configs)
            {
                if (config == null || string.IsNullOrEmpty(config.EndpointPath))
                {
                    continue;
                }

                container[config.DisplayName] = BuildHttpEntry(config);
            }

            string tmpPath = ConfigPath + ".tmp";
            File.WriteAllText(tmpPath, root.ToString(Formatting.Indented));
            if (File.Exists(ConfigPath))
            {
                File.Replace(tmpPath, ConfigPath, ConfigPath + ".bak");
            }
            else
            {
                File.Move(tmpPath, ConfigPath);
            }
#else
            throw new InvalidOperationException("Newtonsoft.Json is required for JSON configurators.");
#endif
        }

        public override string GetManualSnippet(IReadOnlyList<McpServerConfig> configs)
        {
#if JSONNET_EXISTS
            JObject snippet = new JObject();
            foreach (McpServerConfig config in configs)
            {
                if (config == null || string.IsNullOrEmpty(config.EndpointPath))
                {
                    continue;
                }

                snippet[config.DisplayName] = BuildHttpEntry(config);
            }
            return snippet.ToString(Formatting.Indented);
#else
            return "{}";
#endif
        }

#if JSONNET_EXISTS
        private JObject GetServerContainer(JObject root)
        {
            return _vsCodeLayout
                ? root["servers"] as JObject ?? root["mcp"]?["servers"] as JObject
                : root["mcpServers"] as JObject;
        }

        private JObject GetOrCreateServerContainer(JObject root)
        {
            if (_vsCodeLayout)
            {
                if (root["servers"] is not JObject servers)
                {
                    servers = new JObject();
                    root["servers"] = servers;
                }
                return servers;
            }

            if (root["mcpServers"] is not JObject mcpServers)
            {
                mcpServers = new JObject();
                root["mcpServers"] = mcpServers;
            }
            return mcpServers;
        }

        private JObject BuildHttpEntry(McpServerConfig config)
        {
            string url = $"{McpServerProcess.HttpUrl}/{config.EndpointPath}/mcp";
            return new JObject
            {
                ["type"] = "http",
                [_urlProperty] = url,
                ["disabled"] = false
            };
        }
#endif
    }
}
