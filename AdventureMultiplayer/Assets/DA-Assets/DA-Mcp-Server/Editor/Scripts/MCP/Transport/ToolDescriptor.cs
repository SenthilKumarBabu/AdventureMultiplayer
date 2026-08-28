using System.Collections.Generic;

namespace DA_Assets.Shared.MCP
{
    public sealed class ToolDescriptor
    {
        public string name;
        public string description;
        public string endpointPath;
        public string serverName;
        public InputSchema inputSchema;
        public bool requiresPolling;
        public string pollAction;
        public int maxPollSeconds;
    }

    public sealed class ResourceDescriptor
    {
        public string uri;
        public string name;
        public string description;
        public string mimeType;
    }

    public sealed class CommandResult
    {
        public string status;
        public IReadOnlyList<ContentItem> content;
        public string error;

        public static CommandResult Success(IReadOnlyList<ContentItem> content) =>
            new() { status = "success", content = content };

        public static CommandResult Failure(string error) =>
            new() { status = "error", error = error };
    }
}
