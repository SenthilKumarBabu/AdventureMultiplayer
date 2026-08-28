using DA_Assets.Shared.MCP;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    public class SelectFcuInstanceTool : IMcpTool
    {
        private readonly McpToolSO _toolSO;

        public SelectFcuInstanceTool(McpToolSO toolSO)
        {
            _toolSO = toolSO;
        }

        public string Name => _toolSO.ToolName;
        public string Description => _toolSO.ToolDescription;
        public InputSchema InputSchema => _toolSO.Schema;

        public Task<IReadOnlyList<ContentItem>> ExecuteAsync(Dictionary<string, object> args)
        {
            args ??= new Dictionary<string, object>();
            args.TryGetValue("instance_id", out object instanceIdObj);
            string instanceId = Convert.ToString(instanceIdObj);

            bool selected = FcuMcpContext.TrySelect(instanceId, _toolSO.InstanceTypeName, out FcuMcpInstanceInfo selectedInstance);
            string json = JsonUtility.ToJson(new SelectFcuInstanceResult
            {
                selected = selected,
                instance = selectedInstance,
                message = selected
                    ? $"Selected {_toolSO.InstanceLabel} instance '{selectedInstance.name}'."
                    : $"{_toolSO.InstanceLabel} instance '{instanceId}' was not found."
            }, true);

            IReadOnlyList<ContentItem> response = new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = json
                }
            };

            return Task.FromResult(response);
        }

        [Serializable]
        private class SelectFcuInstanceResult
        {
            public bool selected;
            public string message;
            public FcuMcpInstanceInfo instance;
        }
    }
}