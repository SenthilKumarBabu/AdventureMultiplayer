using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

#if JSONNET_EXISTS
using Newtonsoft.Json.Linq;
#endif

namespace DA_Assets.Shared.MCP.Tests
{
    [TestFixture]
    public sealed class McpClientConfiguratorEditModeTests
    {
        private string _tempDir;
        private readonly List<McpServerConfig> _configs = new();

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "DA_McpServerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (McpServerConfig config in _configs)
            {
                if (config != null)
                {
                    UnityEngine.Object.DestroyImmediate(config);
                }
            }

            _configs.Clear();

            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
            }
        }

        [TestCase("url")]
        [TestCase("serverUrl")]
        [TestCase("httpUrl")]
        public void JsonCheckStatus_DetectsConfiguredHttpEntry_ForSupportedUrlProperties(string urlProperty)
        {
#if JSONNET_EXISTS
            string configPath = Path.Combine(_tempDir, $"{urlProperty}.json");
            File.WriteAllText(configPath, "{\"mcpServers\":{\"DA Test\":{\"" + urlProperty + "\":\"http://127.0.0.1:8765/test/mcp\"}}}");

            var configurator = new McpJsonConfigurator("test", "Test", configPath, urlProperty);

            Assert.AreEqual(McpClientStatus.Configured, configurator.CheckStatus());
#else
            Assert.Ignore("Newtonsoft.Json is required for JSON configurator tests.");
#endif
        }

        [Test]
        public void JsonConfigure_WritesOneEntryPerServerAndRemovesLegacySingleServerEntry()
        {
#if JSONNET_EXISTS
            string configPath = Path.Combine(_tempDir, "client.json");
            File.WriteAllText(configPath, "{\"mcpServers\":{\"daMCP\":{\"url\":\"http://old\"},\"External\":{\"url\":\"http://external\"}}}");

            var configurator = new McpJsonConfigurator("test", "Test", configPath, "serverUrl");

            configurator.Configure(new[] { CreateConfig("First Server", "first"), CreateConfig("Second Server", "second") });

            JObject servers = (JObject)JObject.Parse(File.ReadAllText(configPath))["mcpServers"];
            Assert.NotNull(servers["External"]);
            Assert.IsNull(servers["daMCP"]);
            Assert.AreEqual($"{McpServerProcess.HttpUrl}/first/mcp", (string)servers["First Server"]["serverUrl"]);
            Assert.AreEqual($"{McpServerProcess.HttpUrl}/second/mcp", (string)servers["Second Server"]["serverUrl"]);
            Assert.IsFalse((bool)servers["First Server"]["disabled"]);
            Assert.IsFalse((bool)servers["Second Server"]["disabled"]);
#else
            Assert.Ignore("Newtonsoft.Json is required for JSON configurator tests.");
#endif
        }

        [Test]
        public void JsonConfigure_UsesVsCodeServersContainer_WhenVsCodeLayoutEnabled()
        {
#if JSONNET_EXISTS
            string configPath = Path.Combine(_tempDir, "vscode.json");
            var configurator = new McpJsonConfigurator("vscode", "VS Code", configPath, vsCodeLayout: true);

            configurator.Configure(new[] { CreateConfig("VS Server", "vs") });

            JObject root = JObject.Parse(File.ReadAllText(configPath));
            Assert.IsNull(root["mcpServers"]);
            Assert.AreEqual($"{McpServerProcess.HttpUrl}/vs/mcp", (string)root["servers"]["VS Server"]["url"]);
#else
            Assert.Ignore("Newtonsoft.Json is required for JSON configurator tests.");
#endif
        }

        [Test]
        public void CodexConfigure_UpsertsTomlAndPreservesOutsideContent()
        {
            string configPath = Path.Combine(_tempDir, "config.toml");
            File.WriteAllText(configPath, "profile = \"default\"\n\napproval_policy = \"never\"\n");

            var configurator = new McpCodexConfigurator(configPath);

            configurator.Configure(new[] { CreateConfig("Codex Server", "codex") });

            string toml = File.ReadAllText(configPath);
            Assert.That(toml, Does.Contain("profile = \"default\""));
            Assert.That(toml, Does.Contain("approval_policy = \"never\""));
            Assert.That(toml, Does.Contain("[mcp_servers.\"Codex Server\"]"));
            Assert.That(toml, Does.Contain($"url = \"{McpServerProcess.HttpUrl}/codex/mcp\""));
            Assert.That(toml, Does.Contain("disabled = false"));
        }

        [Test]
        public void CodexConfigure_OverwritesExistingServerSection()
        {
            string configPath = Path.Combine(_tempDir, "config.toml");
            File.WriteAllText(configPath,
                "profile = \"default\"\n\n" +
                "[mcp_servers.\"Codex Server\"]\n" +
                "url = \"http://old\"\n\n" +
                "[mcp_servers.external]\n" +
                "url = \"http://external\"\n");

            var configurator = new McpCodexConfigurator(configPath);

            configurator.Configure(new[] { CreateConfig("Codex Server", "codex") });

            string toml = File.ReadAllText(configPath);
            Assert.AreEqual(1, CountOccurrences(toml, "[mcp_servers.\"Codex Server\"]"));
            Assert.That(toml, Does.Not.Contain("http://old"));
            Assert.That(toml, Does.Contain("[mcp_servers.external]"));
            Assert.That(toml, Does.Contain($"url = \"{McpServerProcess.HttpUrl}/codex/mcp\""));
        }

        [Test]
        public void CodexConfigure_RefusesInvalidTomlWithoutChangingConfig()
        {
            string configPath = Path.Combine(_tempDir, "config.toml");
            string original =
                "profile = \"default\"\n\n" +
                "[mcp_servers.\"Codex Server\"]\n" +
                "url = \"http://duplicate\"\n" +
                "[mcp_servers.\"Codex Server\"]\n" +
                "url = \"http://duplicate-again\"\n";
            File.WriteAllText(configPath, original);

            var configurator = new McpCodexConfigurator(configPath);

            Assert.Throws<InvalidOperationException>(() => configurator.Configure(new[] { CreateConfig("Codex Server", "codex") }));

            string toml = File.ReadAllText(configPath);
            Assert.AreEqual(original, toml);
        }

        private McpServerConfig CreateConfig(string displayName, string endpointPath)
        {
            McpServerConfig config = ScriptableObject.CreateInstance<McpServerConfig>();
            config.DisplayName = displayName;
            config.EndpointPath = endpointPath;
            _configs.Add(config);
            return config;
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
