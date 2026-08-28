using DA_Assets.DAI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.Shared.MCP
{
    internal static class McpUi
    {
        private const string SoftBreak = "\u200B";

        public static VisualElement Root(DAInspectorUITK uitk)
        {
            VisualElement root = uitk.CreateRoot(uitk.ColorScheme.BG, false);
            root.style.flexGrow = 1;
            root.style.minHeight = 0;
            return root;
        }

        public static VisualElement Panel(DAInspectorUITK uitk)
        {
            var panel = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = uitk.ColorScheme.GROUP
                }
            };

            UIHelpers.SetDefaultPadding(panel);
            UIHelpers.SetRadius(panel, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(panel, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(panel, uitk.ColorScheme.OUTLINE);
            return panel;
        }

        public static VisualElement Section(DAInspectorUITK uitk, string title, string subtitle = null)
        {
            VisualElement panel = Panel(uitk);
            panel.style.marginBottom = DAI_UitkConstants.MarginPadding;

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = DAI_UitkConstants.MarginPadding
                }
            };

            header.Add(new Label(title)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = DAI_UitkConstants.FontSizeTitle,
                    color = uitk.ColorScheme.TEXT
                }
            });

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                header.Add(new Label(subtitle)
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal,
                        color = uitk.ColorScheme.TEXT_SECOND,
                        marginTop = DAI_UitkConstants.SpacingXXS
                    }
                });
            }

            panel.Add(header);
            return panel;
        }

        public static VisualElement Row(DAInspectorUITK uitk, string label, string value, float labelWidth = 145f)
        {
            return Row(uitk, label, value, out _, labelWidth);
        }

        public static VisualElement Row(DAInspectorUITK uitk, string label, string value, out Label valueLabel, float labelWidth = 145f)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.FlexStart,
                    minWidth = 0,
                    marginTop = DAI_UitkConstants.SpacingXXS,
                    marginBottom = DAI_UitkConstants.SpacingXXS
                }
            };

            row.Add(new Label(label)
            {
                style =
                {
                    minWidth = labelWidth,
                    width = labelWidth,
                    flexShrink = 0,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = uitk.ColorScheme.TEXT_SECOND
                }
            });

            valueLabel = new Label(WrapValue(value))
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = 0,
                    whiteSpace = WhiteSpace.Normal,
                    color = uitk.ColorScheme.TEXT
                }
            };
            row.Add(valueLabel);

            return row;
        }

        private static string WrapValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            return value
                .Replace("\\", "\\" + SoftBreak)
                .Replace("/", "/" + SoftBreak)
                .Replace("-", "-" + SoftBreak)
                .Replace(" ", " " + SoftBreak);
        }

        public sealed class ServerSectionRefs
        {
            public Label ServerStatusValue;
            public Label BridgeStatusValue;
            public CustomButton StartServerButton;
            public CustomButton StopServerButton;
        }

        public static VisualElement ServerSection(
            DAInspectorUITK uitk,
            bool hasServerConfigs,
            bool localServerRunning,
            Action startServer,
            Action stopServer,
            out ServerSectionRefs refs)
        {
            refs = new ServerSectionRefs();

            VisualElement server = Section(uitk, McpLocKey.section_server_title.Localize());
            server.Add(Row(uitk, McpLocKey.label_transport.Localize(), McpLocKey.value_http_local.Localize()));
            server.Add(Row(uitk, McpLocKey.label_endpoint.Localize(), McpServerProcess.McpUrl));
            if (!hasServerConfigs)
            {
                server.Add(HelpBox(uitk, McpLocKey.warning_no_mcp_servers.Localize(), MessageType.Warning));
            }

            server.Add(Row(uitk, McpLocKey.label_local_server.Localize(), ServerStatusText(localServerRunning), out refs.ServerStatusValue));
            server.Add(Row(uitk, McpLocKey.label_unity_bridge.Localize(), BridgeStatusText(), out refs.BridgeStatusValue));

            refs.StartServerButton = Button(
                uitk,
                McpLocKey.button_start_server.Localize(),
                startServer,
                hasServerConfigs && !localServerRunning,
                uitk.ColorScheme.GREEN);
            refs.StopServerButton = Button(
                uitk,
                McpLocKey.button_stop_server.Localize(),
                stopServer,
                localServerRunning,
                uitk.ColorScheme.RED);

            server.Add(ButtonRow(refs.StartServerButton, refs.StopServerButton));
            return server;
        }

        public static void UpdateServerSection(ServerSectionRefs refs, bool hasServerConfigs, bool localServerRunning)
        {
            if (refs == null)
            {
                return;
            }

            if (refs.ServerStatusValue != null)
            {
                refs.ServerStatusValue.text = ServerStatusText(localServerRunning);
            }

            if (refs.BridgeStatusValue != null)
            {
                refs.BridgeStatusValue.text = BridgeStatusText();
            }

            refs.StartServerButton?.SetEnabled(hasServerConfigs && !localServerRunning);
            refs.StopServerButton?.SetEnabled(localServerRunning);
        }

        public static VisualElement Notice(DAInspectorUITK uitk, string message, MessageType type = MessageType.Info)
        {
            Color stripeColor = type switch
            {
                MessageType.Warning => Color.yellow,
                MessageType.Error => uitk.ColorScheme.RED,
                _ => uitk.ColorScheme.ACCENT_SECOND
            };

            var box = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Stretch,
                    backgroundColor = uitk.ColorScheme.BUTTON,
                    marginTop = DAI_UitkConstants.MarginPadding
                }
            };

            UIHelpers.SetRadius(box, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(box, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(box, uitk.ColorScheme.OUTLINE);

            box.Add(new VisualElement
            {
                style =
                {
                    width = DAI_UitkConstants.SpacingXS,
                    backgroundColor = stripeColor,
                    flexShrink = 0
                }
            });

            box.Add(new Label(message)
            {
                style =
                {
                    flexGrow = 1,
                    whiteSpace = WhiteSpace.Normal,
                    color = uitk.ColorScheme.TEXT,
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    paddingRight = DAI_UitkConstants.MarginPadding,
                    paddingBottom = DAI_UitkConstants.MarginPadding,
                    paddingLeft = DAI_UitkConstants.MarginPadding
                }
            });

            return box;
        }

        public static VisualElement ListRow(DAInspectorUITK uitk, string primary, string secondary = null)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    minHeight = 24,
                    paddingLeft = DAI_UitkConstants.SpacingM,
                    paddingRight = DAI_UitkConstants.SpacingM,
                    paddingTop = DAI_UitkConstants.SpacingXXS,
                    paddingBottom = DAI_UitkConstants.SpacingXXS
                }
            };

            row.Add(new Label(primary)
            {
                style =
                {
                    flexGrow = 1,
                    whiteSpace = WhiteSpace.Normal,
                    color = uitk.ColorScheme.TEXT,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            });

            if (!string.IsNullOrWhiteSpace(secondary))
            {
                row.Add(new Label(secondary)
                {
                    style =
                    {
                        marginLeft = DAI_UitkConstants.MarginPadding,
                        color = uitk.ColorScheme.TEXT_SECOND,
                        unityTextAlign = TextAnchor.MiddleRight,
                        flexShrink = 0
                    }
                });
            }

            return row;
        }

        public static VisualElement Pill(DAInspectorUITK uitk, string text, Color color)
        {
            var pill = new Label(text)
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = DAI_UitkConstants.FontSizeTiny,
                    color = Color.white,
                    backgroundColor = color,
                    paddingLeft = DAI_UitkConstants.MarginPadding,
                    paddingRight = DAI_UitkConstants.MarginPadding,
                    paddingTop = DAI_UitkConstants.SpacingXXS,
                    paddingBottom = DAI_UitkConstants.SpacingXXS,
                    alignSelf = Align.FlexStart,
                    flexGrow = 0,
                    flexShrink = 0
                }
            };

            UIHelpers.SetRadius(pill, DAI_UitkConstants.CornerRadiusSmall);
            return pill;
        }

        public static CustomButton Button(DAInspectorUITK uitk, string text, System.Action action, bool enabled = true, Color? color = null)
        {
            CustomButton button = uitk.Button(text, action);
            button.SetEnabled(enabled);
            button.style.flexGrow = 0;
            button.style.flexShrink = 0;
            button.style.minWidth = 110;

            if (color.HasValue)
            {
                button.color = color.Value;
            }

            return button;
        }

        public static CustomHelpBox HelpBox(DAInspectorUITK uitk, string message, MessageType type)
        {
            return uitk.HelpBox(new HelpBoxData
            {
                Message = message,
                MessageType = type
            });
        }

        public static AnimatedFoldout Foldout(DAInspectorUITK uitk, string id, string title, VisualElement body, bool expanded = true)
        {
            VisualElement header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = uitk.ColorScheme.BUTTON
                }
            };
            UIHelpers.SetDefaultPadding(header);
            UIHelpers.SetRadius(header, DAI_UitkConstants.CornerRadius);

            header.Add(new Label(title)
            {
                style =
                {
                    flexGrow = 1,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = uitk.ColorScheme.TEXT
                }
            });

            AnimatedFoldout foldout = new AnimatedFoldout(id, header, body, expanded, uitk.FoldoutCurve, uitk.FoldoutDuration, uitk.ColorScheme.GROUP);
            foldout.style.marginTop = DAI_UitkConstants.SpacingXS;
            foldout.style.marginBottom = DAI_UitkConstants.SpacingXS;
            return foldout;
        }

        public static VisualElement ButtonRow(params VisualElement[] buttons)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexWrap = Wrap.Wrap,
                    marginTop = DAI_UitkConstants.MarginPadding
                }
            };

            foreach (VisualElement button in buttons)
            {
                button.style.marginRight = DAI_UitkConstants.SpacingXS;
                button.style.marginBottom = DAI_UitkConstants.SpacingXS;
                row.Add(button);
            }

            return row;
        }

        public sealed class ClientsSectionState
        {
            public IReadOnlyList<McpClientConfigurator> Clients;
            public int SelectedClientIndex;
            public bool HasServerConfigs;
            public Func<IReadOnlyList<McpServerConfig>> GetServerConfigs;
            public Action<int> SelectClient;
            public Action ConfigureAllDetectedClients;
            public Action Rebuild;
        }

        public static VisualElement ClientsSection(DAInspectorUITK uitk, ClientsSectionState state)
        {
            VisualElement section = Section(uitk, McpLocKey.section_client_configuration_title.Localize());
            IReadOnlyList<McpClientConfigurator> clients = state.Clients ?? Array.Empty<McpClientConfigurator>();

            if (!state.HasServerConfigs)
            {
                section.Add(HelpBox(uitk, McpLocKey.warning_no_mcp_servers.Localize(), MessageType.Warning));
            }

            if (clients.Count == 0)
            {
                section.Add(Notice(uitk, McpLocKey.notice_no_supported_clients.Localize(), MessageType.Warning));
                section.Add(ButtonRow(Button(
                    uitk,
                    McpLocKey.button_configure_all_detected_clients.Localize(),
                    state.ConfigureAllDetectedClients,
                    state.HasServerConfigs,
                    uitk.ColorScheme.GREEN)));
                return section;
            }

            int selectedIndex = Mathf.Clamp(state.SelectedClientIndex, 0, clients.Count - 1);
            McpClientConfigurator client = clients[selectedIndex];
            McpClientStatus status = client.CheckStatus();
            string[] names = clients.Select(x => x.DisplayName).ToArray();
            var popup = new PopupField<string>(McpLocKey.label_client.Localize(), names.ToList(), selectedIndex);
            popup.RegisterValueChangedCallback(evt =>
            {
                int index = names.ToList().IndexOf(evt.newValue);
                state.SelectClient?.Invoke(index);
                state.Rebuild?.Invoke();
            });
            popup.style.flexGrow = 1;
            popup.style.flexShrink = 1;
            popup.style.minWidth = 0;

            var clientRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = DAI_UitkConstants.MarginPadding,
                    minWidth = 0
                }
            };

            VisualElement badge = Pill(
                uitk,
                status == McpClientStatus.Configured ? McpLocKey.status_configured.Localize() : McpLocKey.status_needs_setup.Localize(),
                status == McpClientStatus.Configured ? uitk.ColorScheme.GREEN : uitk.ColorScheme.RED);
            badge.style.marginRight = DAI_UitkConstants.MarginPadding;

            clientRow.Add(badge);
            clientRow.Add(popup);
            section.Add(clientRow);
            section.Add(ConfigPathField(uitk, client));
            section.Add(ButtonRow(
                Button(uitk, McpLocKey.button_configure.Localize(), () =>
                {
                    client.Configure(state.GetServerConfigs());
                    state.Rebuild?.Invoke();
                }, state.HasServerConfigs && client.IsInstalled && client.SupportsAutoConfigure, uitk.ColorScheme.ACCENT_SECOND),
                Button(uitk, McpLocKey.button_copy_config.Localize(), () => EditorGUIUtility.systemCopyBuffer = client.GetManualSnippet(state.GetServerConfigs()), state.HasServerConfigs)));
            section.Add(ButtonRow(Button(
                uitk,
                McpLocKey.button_configure_all_detected_clients.Localize(),
                state.ConfigureAllDetectedClients,
                state.HasServerConfigs,
                uitk.ColorScheme.GREEN)));

            section.Add(new Label(McpLocKey.label_manual_configuration.Localize())
            {
                style =
                {
                    marginTop = DAI_UitkConstants.MarginPadding,
                    marginBottom = DAI_UitkConstants.SpacingXS,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = uitk.ColorScheme.TEXT_SECOND
                }
            });

            TextField snippet = uitk.TextField(string.Empty, null);
            snippet.multiline = true;
            snippet.value = client.GetManualSnippet(state.GetServerConfigs());
            snippet.isReadOnly = true;
            snippet.style.height = 130;
            snippet.style.flexGrow = 1;
            if (snippet.childCount > 1)
            {
                snippet[0].style.display = DisplayStyle.None;
                snippet[1].style.marginLeft = 0;
                snippet[1].style.maxWidth = StyleKeyword.None;
            }
            section.Add(snippet);

            return section;
        }

        private static VisualElement ConfigPathField(DAInspectorUITK uitk, McpClientConfigurator client)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginTop = DAI_UitkConstants.MarginPadding,
                    marginBottom = DAI_UitkConstants.SpacingXS
                }
            };

            container.Add(new Label($"{McpLocKey.label_config_path.Localize()}:")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = uitk.ColorScheme.TEXT_SECOND,
                    marginBottom = DAI_UitkConstants.SpacingXS
                }
            });

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    minWidth = 0
                }
            };

            TextField path = uitk.TextField(string.Empty, null);
            path.value = client.ConfigPath;
            path.isReadOnly = true;
            path.style.flexGrow = 1;
            path.style.flexShrink = 1;
            path.style.minWidth = 0;
            path.style.marginRight = DAI_UitkConstants.MarginPadding;
            if (path.childCount > 1)
            {
                path[0].style.display = DisplayStyle.None;
                path[1].style.marginLeft = 0;
                path[1].style.maxWidth = StyleKeyword.None;
            }

            CustomButton copy = Button(uitk, McpLocKey.button_copy_path.Localize(), () => EditorGUIUtility.systemCopyBuffer = client.ConfigPath);
            copy.style.minWidth = 72;
            copy.style.marginRight = DAI_UitkConstants.SpacingXS;

            CustomButton open = Button(uitk, McpLocKey.button_open.Localize(), () => EditorUtility.RevealInFinder(client.ConfigPath), client.IsInstalled);
            open.style.minWidth = 72;

            row.Add(path);
            row.Add(copy);
            row.Add(open);
            container.Add(row);
            return container;
        }

        private static string ServerStatusText(bool localServerRunning)
        {
            return localServerRunning ? McpLocKey.status_running.Localize() : McpLocKey.status_stopped.Localize();
        }

        private static string BridgeStatusText()
        {
            return TransportManager.IsHttpConnected ? McpLocKey.status_connected.Localize() : McpLocKey.status_disconnected.Localize();
        }
    }
}
