using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, IMcpTool> _tools = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, McpToolSO> _toolAssets = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IMcpResource> _resources = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ResourceDescriptor> _resourceDescriptors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ToolDescriptor> _toolDescriptors = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<ToolDescriptor> ToolDescriptors => _toolDescriptors.Values;
        public IReadOnlyCollection<ResourceDescriptor> ResourceDescriptors => _resourceDescriptors.Values;

        public void Rebuild()
        {
            _tools.Clear();
            _toolAssets.Clear();
            _resources.Clear();
            _resourceDescriptors.Clear();
            _toolDescriptors.Clear();

            if (McpConfig.Instance.McpRegistry == null)
            {
                return;
            }

            foreach (McpServerConfig config in McpConfig.Instance.McpRegistry.Servers.OfType<McpServerConfig>())
            {
                RegisterConfig(config);
            }
        }

        public async Task<CommandResult> ExecuteAsync(string endpointPath, string toolName, Dictionary<string, object> args)
        {
            if (string.Equals(toolName, "read_resource", StringComparison.OrdinalIgnoreCase))
            {
                return await ReadResourceAsync(args);
            }

            string dispatchKey = $"{endpointPath}:{toolName}";

            if (!_tools.TryGetValue(dispatchKey, out IMcpTool tool))
            {
                McpRecentCalls.Add(toolName, false, "Tool not found.");
                return CommandResult.Failure($"Tool '{toolName}' not found (endpoint: {endpointPath}).");
            }

            if (_toolAssets.TryGetValue(dispatchKey, out McpToolSO toolSO))
            {
                string gateError = ValidateGates(toolName, toolSO);
                if (!string.IsNullOrEmpty(gateError))
                {
                    McpRecentCalls.Add(toolName, false, gateError);
                    return CommandResult.Failure(gateError);
                }
            }

            try
            {
                IReadOnlyList<ContentItem> content = await tool.ExecuteAsync(args ?? new Dictionary<string, object>());
                McpRecentCalls.Add(toolName, true, null);
                return CommandResult.Success(content);
            }
            catch (Exception ex)
            {
                McpRecentCalls.Add(toolName, false, ex.Message);
                return CommandResult.Failure(ex.Message);
            }
        }

        private void RegisterConfig(McpServerConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.EndpointPath))
            {
                return;
            }

            foreach (McpToolSO toolSO in config.McpTools.Where(x => x != null))
            {
                string toolName = toolSO.ToolName;
                string dispatchKey = $"{config.EndpointPath}:{toolName}";
                if (_tools.ContainsKey(dispatchKey))
                {
                    continue;
                }

                IMcpTool tool = toolSO.CreateInstance(null);
                _tools.Add(dispatchKey, tool);
                _toolAssets.Add(dispatchKey, toolSO);
                _toolDescriptors.Add(dispatchKey, BuildDescriptor(config, toolName, toolSO, tool));
            }

            foreach (McpResourceSO resourceSO in config.McpResources.Where(x => x != null))
            {
                IMcpResource resource = resourceSO.CreateInstance(null);
                _resources[resource.Uri] = resource;
                _resourceDescriptors[resource.Uri] = new ResourceDescriptor
                {
                    uri = resource.Uri,
                    name = resource.Name,
                    description = resource.Description,
                    mimeType = resource.MimeType
                };
            }
        }

        private async Task<CommandResult> ReadResourceAsync(Dictionary<string, object> args)
        {
            string uri = args != null && args.TryGetValue("uri", out object value) ? value?.ToString() : null;
            if (string.IsNullOrWhiteSpace(uri) || !_resources.TryGetValue(uri, out IMcpResource resource))
            {
                return CommandResult.Failure($"Resource '{uri}' not found.");
            }

            IReadOnlyList<ResourceContentItem> contents = await resource.ReadAsync();
            return CommandResult.Success(contents.Select(x => new ContentItem
            {
                Type = "text",
                Text = x.Text
            }).ToList());
        }

        private static string ValidateGates(string toolName, McpToolSO toolSO)
        {
            bool isPlaying = Application.isPlaying;
            if (toolSO.RunMode == RunMode.EditOnly && isPlaying)
            {
                return $"Tool '{toolName}' requires Edit Mode.";
            }
            if (toolSO.RunMode == RunMode.PlayOnly && !isPlaying)
            {
                return $"Tool '{toolName}' requires Play Mode.";
            }
            if (toolSO.RequiresGui && Application.isBatchMode)
            {
                return $"Tool '{toolName}' requires Unity Editor GUI.";
            }

            return null;
        }

        private static ToolDescriptor BuildDescriptor(McpServerConfig config, string toolName, McpToolSO toolSO, IMcpTool tool)
        {
            return new ToolDescriptor
            {
                name = toolName,
                description = tool.Description,
                endpointPath = config.EndpointPath,
                serverName = config.DisplayName,
                inputSchema = tool.InputSchema,
                requiresPolling = toolSO.RequiresPolling,
                pollAction = string.IsNullOrWhiteSpace(toolSO.PollAction) ? "status" : toolSO.PollAction,
                maxPollSeconds = toolSO.MaxPollSeconds
            };
        }
    }
}
