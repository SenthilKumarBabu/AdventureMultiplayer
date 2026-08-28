using DA_Assets.DAI;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.Shared.MCP
{
    public sealed class McpAssetTab : VisualElement
    {
        private readonly DAInspectorUITK _uitk;
        private readonly McpServerConfig _config;
        private readonly McpClientConfigurator[] _clients;
        private IVisualElementScheduledItem _refresh;
        private int _selectedClientIndex;
        private double _lastLocalServerPollTime;
        private bool _lastLocalServerRunning;
        private McpUi.ServerSectionRefs _serverSectionRefs;

        public McpAssetTab(McpServerConfig config, DAInspectorUITK uitk)
        {
            _config = config;
            _uitk = uitk;
            _clients = McpClientRegistry.All().ToArray();

            style.flexGrow = 1;
            style.minHeight = 0;

            Build();
            _refresh = schedule.Execute(UpdateStatusWidgets).Every(1000);
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                _refresh?.Pause();
                _refresh = null;
            });

        }

        private void Build()
        {
            Clear();

            if (_uitk == null)
            {
                Add(new Label(McpLocKey.missing_uitk.Localize()));
                return;
            }

            style.backgroundColor = _uitk.ColorScheme.BG;
            UIHelpers.SetDefaultPadding(this);

            if (_config == null)
            {
                Add(McpUi.HelpBox(_uitk, McpLocKey.warning_mcp_config_not_assigned.Localize(), MessageType.Warning));
                return;
            }

            AddContent();
        }

        private void AddContent()
        {
            RefreshLocalServerState();
            Add(McpUi.ServerSection(_uitk, HasServerConfigs(), _lastLocalServerRunning, StartServer, StopServer, out _serverSectionRefs));
            Add(McpUi.ClientsSection(_uitk, new McpUi.ClientsSectionState
            {
                Clients = _clients,
                SelectedClientIndex = _selectedClientIndex,
                HasServerConfigs = HasServerConfigs(),
                GetServerConfigs = GetServerConfigs,
                SelectClient = index => _selectedClientIndex = index,
                ConfigureAllDetectedClients = ConfigureAllDetectedClients,
                Rebuild = Build
            }));
            UpdateStatusWidgets();
        }

        private void UpdateStatusWidgets()
        {
            if (_uitk == null)
            {
                return;
            }

            RefreshLocalServerState();

            bool hasServerConfigs = HasServerConfigs();
            McpUi.UpdateServerSection(_serverSectionRefs, hasServerConfigs, _lastLocalServerRunning);
        }

        private void RefreshLocalServerState()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastLocalServerPollTime <= 0.75f)
            {
                return;
            }

            _lastLocalServerPollTime = now;
            _lastLocalServerRunning = McpServerProcess.IsLocalHttpServerReachable();
        }

        private void StartServer()
        {
            _ = McpServerProcess.StartAndConnectAsync();
            _lastLocalServerPollTime = 0;
            UpdateStatusWidgets();
        }

        private void StopServer()
        {
            _ = McpServerProcess.StopAndDisconnectAsync();
            _lastLocalServerPollTime = 0;
            UpdateStatusWidgets();
        }

        private void ConfigureAllDetectedClients()
        {
            if (!HasServerConfigs())
            {
                return;
            }

            foreach (McpClientConfigurator client in _clients)
            {
                if (client.IsInstalled && client.SupportsAutoConfigure)
                {
                    client.Configure(GetServerConfigs());
                }
            }

            Build();
        }

        private System.Collections.Generic.IReadOnlyList<McpServerConfig> GetServerConfigs()
        {
            return _config == null
                ? System.Array.Empty<McpServerConfig>()
                : new[] { _config };
        }

        private bool HasServerConfigs()
        {
            return GetServerConfigs().Count > 0;
        }
    }
}
