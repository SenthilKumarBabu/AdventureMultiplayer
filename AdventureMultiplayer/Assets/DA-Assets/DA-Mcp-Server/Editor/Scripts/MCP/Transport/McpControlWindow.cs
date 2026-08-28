using DA_Assets.DAI;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UpdateAssetType = DA_Assets.UpdateChecker.Models.AssetType;

namespace DA_Assets.Shared.MCP
{
    public sealed class McpControlWindow : EditorWindow
    {
        [SerializeField] DAInspectorUITK _uitk;

        private readonly Dictionary<string, TabItem> _tabs = new();
        private readonly List<Editor> _embeddedEditors = new();
        private ScrollView _content;
        private string _tab = "Connect";
        private McpClientConfigurator[] _clients;
        private int _selectedClientIndex;
        private IVisualElementScheduledItem _statusRefresh;
        private double _lastLocalServerPollTime;
        private bool _lastLocalServerRunning;

        private McpUi.ServerSectionRefs _serverSectionRefs;

        private static readonly string[] TabNames =
        {
            "Connect",
            "Clients",
            "Instances",
            "Tools",
            "Resources",
            "Recent Calls"
        };

        public static void ShowWindow()
        {
            McpConfig.Instance.Localizator.Init();
            var window = GetWindow<McpControlWindow>(McpLocKey.window_title.Localize());
            window.titleContent = new GUIContent(McpLocKey.window_title.Localize());
            window.minSize = new Vector2(820, 560);
            window.Show();
        }

        private void OnEnable()
        {
            Build();
        }

        private void OnDisable()
        {
            _statusRefresh?.Pause();
            _statusRefresh = null;
            ClearEmbeddedEditors();
        }

        private void Build()
        {
            McpConfig.Instance.Localizator.Init();
            titleContent = new GUIContent(McpLocKey.window_title.Localize());

            rootVisualElement.Clear();
            _tabs.Clear();
            _statusRefresh?.Pause();

            if (_uitk == null)
            {
                rootVisualElement.Add(new Label(McpLocKey.missing_uitk.Localize()));
                return;
            }

            _clients = McpClientRegistry.All().ToArray();

            VisualElement root = McpUi.Root(_uitk);
            rootVisualElement.Add(root);

            root.Add(BuildHeader());
            root.Add(BuildTopTabs());

            _content = _uitk.ScrollView();
            _content.style.flexGrow = 1;
            _content.style.minHeight = 0;
            root.Add(_content);
            root.Add(BuildFooter());

            SelectTab(_tab);
            _statusRefresh = root.schedule.Execute(UpdateStatusWidgets).Every(1000);
        }

        private VisualElement BuildHeader()
        {
            VisualElement header = McpUi.Panel(_uitk);
            header.style.marginBottom = DAI_UitkConstants.MarginPadding;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.paddingTop = DAI_UitkConstants.SpacingL;
            header.style.paddingBottom = DAI_UitkConstants.SpacingL;

            VisualElement title = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1, minWidth = 0 } };
            title.Add(new Label(McpLocKey.header_title.Localize())
            {
                style =
                {
                    fontSize = 22,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = _uitk.ColorScheme.TEXT
                }
            });
            title.Add(new Label(McpLocKey.header_subtitle.Localize())
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    color = _uitk.ColorScheme.TEXT_SECOND,
                    marginTop = DAI_UitkConstants.SpacingXXS
                }
            });
            header.Add(title);

            return header;
        }

        private VisualElement BuildFooter()
        {
            return _uitk.CreateFooterWithVersionInfo(
                McpConfig.Instance.Localizator.Language,
                new DAInspectorUITK.FooterAssetInfo
                {
                    AssetType = UpdateAssetType.DAMCP,
                    ProductVersion = McpConfig.Instance.ProductVersion
                });
        }

        private VisualElement BuildTopTabs()
        {
            VisualElement tabs = McpUi.Panel(_uitk);
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.flexWrap = Wrap.Wrap;
            tabs.style.marginBottom = DAI_UitkConstants.MarginPadding;
            tabs.style.paddingTop = DAI_UitkConstants.SpacingS;
            tabs.style.paddingBottom = DAI_UitkConstants.SpacingS;

            foreach (string tabName in TabNames)
            {
                TabItem tab = _uitk.TabItem(LocalizedTabName(tabName), () => SelectTab(tabName));
                tab.style.width = Length.Percent(25);
                tab.style.minWidth = 0;
                tab.style.marginTop = 0;
                tab.style.marginBottom = DAI_UitkConstants.SpacingXXS;
                tab.style.marginLeft = 0;
                tab.style.marginRight = 0;
                _tabs[tabName] = tab;
                tabs.Add(tab);
            }

            return tabs;
        }

        private static string LocalizedTabName(string tabName)
        {
            return tabName switch
            {
                "Clients" => McpLocKey.tab_clients.Localize(),
                "Instances" => McpLocKey.tab_instances.Localize(),
                "Tools" => McpLocKey.tab_tools.Localize(),
                "Resources" => McpLocKey.tab_resources.Localize(),
                "Recent Calls" => McpLocKey.tab_recent_calls.Localize(),
                _ => McpLocKey.tab_connect.Localize()
            };
        }

        private void SelectTab(string tabName)
        {
            _tab = tabName;

            foreach (KeyValuePair<string, TabItem> entry in _tabs)
            {
                entry.Value.SetSelected(entry.Key == _tab);
            }

            RebuildContent();
        }

        private void RebuildContent()
        {
            if (_content == null)
            {
                return;
            }

            ClearEmbeddedEditors();
            ClearDynamicRefs();
            _content.Clear();

            switch (_tab)
            {
                case "Clients":
                    DrawClients(_content);
                    break;
                case "Instances":
                    DrawInstances(_content);
                    break;
                case "Tools":
                    DrawTools(_content);
                    break;
                case "Resources":
                    DrawResources(_content);
                    break;
                case "Recent Calls":
                    DrawRecentCalls(_content);
                    break;
                default:
                    DrawConnect(_content);
                    break;
            }

            UpdateStatusWidgets();
        }

        private void ClearDynamicRefs()
        {
            _serverSectionRefs = null;
        }

        private void UpdateStatusWidgets()
        {
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

        private void DrawConnect(VisualElement root)
        {
            RefreshLocalServerState();
            root.Add(McpUi.ServerSection(_uitk, HasServerConfigs(), _lastLocalServerRunning, StartServer, StopServer, out _serverSectionRefs));
        }

        private void DrawTools(VisualElement root)
        {
            VisualElement section = McpUi.Section(_uitk, McpLocKey.section_registered_tools_title.Localize());
            List<McpServerConfig> configs = GetServerConfigs()
                .OrderBy(x => x.DisplayName)
                .ToList();

            if (configs.Sum(x => x.McpTools.Count(y => y != null)) == 0)
            {
                section.Add(McpUi.Notice(_uitk, McpLocKey.notice_no_tools_registered.Localize()));
            }

            foreach (McpServerConfig config in configs)
            {
                VisualElement body = new VisualElement();
                body.Add(BuildSerializedProperty(config, nameof(McpServerConfig.McpTools)));

                section.Add(McpUi.Foldout(_uitk, $"mcp-tools-{config.EndpointPath}", $"{config.DisplayName} ({config.McpTools.Count(x => x != null)})", body));
            }

            section.Add(McpUi.ButtonRow(McpUi.Button(_uitk, McpLocKey.button_re_register_tools.Localize(), () =>
            {
                _ = TransportManager.StopHttpAsync().ContinueWith(_ => TransportManager.StartHttpAsync(McpServerProcess.HttpUrl));
            })));
            root.Add(section);
        }

        private void DrawClients(VisualElement root)
        {
            root.Add(McpUi.ClientsSection(_uitk, new McpUi.ClientsSectionState
            {
                Clients = _clients,
                SelectedClientIndex = _selectedClientIndex,
                HasServerConfigs = HasServerConfigs(),
                GetServerConfigs = GetServerConfigs,
                SelectClient = index => _selectedClientIndex = index,
                ConfigureAllDetectedClients = ConfigureAllDetectedClients,
                Rebuild = RebuildContent
            }));
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

            RebuildContent();
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

        private void DrawResources(VisualElement root)
        {
            VisualElement section = McpUi.Section(_uitk, McpLocKey.section_mcp_resources_title.Localize());
            foreach (McpServerConfig config in GetServerConfigs().OrderBy(x => x.DisplayName))
            {
                VisualElement body = new VisualElement();
                body.Add(BuildSerializedProperty(config, nameof(McpServerConfig.McpResources)));

                section.Add(McpUi.Foldout(_uitk, $"mcp-resources-{config.EndpointPath}", $"{config.DisplayName} ({config.McpResources.Count(x => x != null)})", body));
            }
            root.Add(section);
        }

        private void DrawRecentCalls(VisualElement root)
        {
            VisualElement section = McpUi.Section(_uitk, McpLocKey.section_recent_calls_title.Localize());
            IReadOnlyList<McpRecentCall> calls = McpRecentCalls.List();
            if (calls.Count == 0)
            {
                section.Add(McpUi.Notice(_uitk, McpLocKey.notice_no_recent_calls.Localize()));
            }

            foreach (McpRecentCall call in calls.Take(50))
            {
                string status = call.Success ? McpLocKey.status_ok.Localize() : McpLocKey.status_error.Localize();
                string detail = call.Success ? call.ToolName : $"{call.ToolName} | {call.Error}";
                section.Add(McpUi.Row(_uitk, $"{call.Time:HH:mm:ss}  {status}", detail));
            }
            root.Add(section);
        }

        private void DrawInstances(VisualElement root)
        {
            VisualElement assets = McpUi.Section(_uitk, McpLocKey.section_asset_configs_title.Localize());
            if (McpConfig.Instance.McpRegistry == null)
            {
                assets.Add(McpUi.HelpBox(_uitk, McpLocKey.warning_no_mcp_servers.Localize(), MessageType.Warning));
            }
            else
            {
                assets.Add(BuildInspector(McpConfig.Instance.McpRegistry));
                if (!HasServerConfigs())
                {
                    assets.Add(McpUi.HelpBox(_uitk, McpLocKey.warning_no_mcp_servers.Localize(), MessageType.Warning));
                }
            }
            root.Add(assets);

            VisualElement instances = McpUi.Section(_uitk, McpLocKey.section_live_instances_title.Localize());
            IReadOnlyList<McpInstanceDescriptor> list = McpInstanceRegistry.List();
            if (list.Count == 0)
            {
                instances.Add(McpUi.Notice(_uitk, McpLocKey.notice_no_instances.Localize()));
            }

            foreach (IGrouping<string, McpInstanceDescriptor> group in list.GroupBy(x => x.TypeName).OrderBy(x => x.Key))
            {
                VisualElement body = new VisualElement();
                foreach (McpInstanceDescriptor instance in group)
                {
                    body.Add(McpUi.Row(_uitk, instance.DisplayName, $"{instance.Id} | {instance.Path}", 210f));
                }

                instances.Add(McpUi.Foldout(_uitk, $"mcp-instances-{group.Key}", $"{group.Key} ({group.Count()})", body));
            }
            root.Add(instances);
        }

        private VisualElement BuildInspector(Object target)
        {
            Editor editor = Editor.CreateEditor(target);
            _embeddedEditors.Add(editor);

            var container = new VisualElement
            {
                style =
                {
                    marginTop = DAI_UitkConstants.SpacingXS,
                    marginBottom = DAI_UitkConstants.SpacingXS,
                    paddingLeft = DAI_UitkConstants.SpacingXS,
                    paddingRight = DAI_UitkConstants.SpacingXS
                }
            };

            container.Add(new IMGUIContainer(() =>
            {
                if (editor != null)
                {
                    editor.OnInspectorGUI();
                }
            }));

            return container;
        }

        private VisualElement BuildSerializedProperty(Object target, string propertyName)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            var container = new VisualElement
            {
                style =
                {
                    marginTop = DAI_UitkConstants.SpacingXS,
                    marginBottom = DAI_UitkConstants.SpacingXS,
                    paddingLeft = DAI_UitkConstants.SpacingXS,
                    paddingRight = DAI_UitkConstants.SpacingXS
                }
            };

            container.Add(new IMGUIContainer(() =>
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(property, true);
                serializedObject.ApplyModifiedProperties();
            }));

            return container;
        }

        private void ClearEmbeddedEditors()
        {
            foreach (Editor editor in _embeddedEditors)
            {
                if (editor != null)
                {
                    DestroyImmediate(editor);
                }
            }

            _embeddedEditors.Clear();
        }

        private static IReadOnlyList<McpServerConfig> GetServerConfigs()
        {
            return McpConfig.Instance.McpRegistry == null
                ? new List<McpServerConfig>()
                : McpConfig.Instance.McpRegistry.Servers.OfType<McpServerConfig>().ToList();
        }

        private static bool HasServerConfigs()
        {
            return GetServerConfigs().Count > 0;
        }
    }
}
