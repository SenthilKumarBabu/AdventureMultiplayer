using DA_Assets.Tools;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    public abstract class McpToolSO : ScriptableObject
    {
        [Header("Tool Identity")]
        public string ToolName;

        [TextArea(2, 4)]
        public string ToolDescription;

        [Header("Instance Context")]
        public string InstanceTypeName;
        public string InstanceLabel = "MCP";

        [Header("Input Schema")]
        public InputSchema Schema;

        [Header("Response Templates")]
        public SerializedDictionary<string, string> ResponseTemplates = new();

        [Header("Long-running Tools")]
        public bool RequiresPolling;
        public string PollAction = "status";
        public int MaxPollSeconds;

        [Header("Lifecycle")]
        public RunMode RunMode = RunMode.EditOnly;
        public InstanceRequirement Requirement = InstanceRequirement.Required;
        public bool SupportsCancellation = true;
        public bool ReportsProgress;
        public bool RequiresGui;

        public abstract IMcpTool CreateInstance(object context);

        public string GetTemplate(string key)
        {
            return ResponseTemplates != null && ResponseTemplates.TryGetValue(key, out var template)
                ? template
                : string.Empty;
        }

        public string FormatTemplate(string key, params object[] args)
        {
            string template = GetTemplate(key);
            return string.IsNullOrEmpty(template) ? string.Empty : string.Format(template, args);
        }
    }
}
