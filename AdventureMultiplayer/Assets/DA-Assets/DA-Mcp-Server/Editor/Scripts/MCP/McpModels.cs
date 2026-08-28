using System;
using System.Collections.Generic;
using DA_Assets.Tools;
using UnityEngine;

#if JSONNET_EXISTS
using Newtonsoft.Json;
#endif

namespace DA_Assets.Shared.MCP
{
    public struct JsonRpcRequest
    {
#if JSONNET_EXISTS
        [JsonProperty("jsonrpc")] 
#endif
        public string JsonRpc { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("id")] 
#endif
        public object Id { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("method")]
#endif
        public string Method { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("params")] 
#endif
        public object Params { get; set; }
    }

    public struct JsonRpcResponse<T>
    {
#if JSONNET_EXISTS
        [JsonProperty("jsonrpc")] 
#endif
        public string JsonRpc { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("id")] 
#endif
        public object Id { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("result")] 
#endif
        public T Result { get; set; }
    }

    public struct JsonRpcErrorResponse
    {
#if JSONNET_EXISTS
        [JsonProperty("jsonrpc")] 
#endif
        public string JsonRpc { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("id")] 
#endif
        public object Id { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("error")] 
#endif
        public RpcError Error { get; set; }
    }

    public struct RpcError
    {
#if JSONNET_EXISTS
        [JsonProperty("code")] 
#endif
        public int Code { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("message")] 
#endif
        public string Message { get; set; }
    }

    public struct InitializeResult
    {
#if JSONNET_EXISTS
        [JsonProperty("protocolVersion")] 
#endif
        public string ProtocolVersion { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("capabilities")] 
#endif
        public ServerCapabilities Capabilities { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("serverInfo")] 
#endif
        public ServerInfo ServerInfo { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("instructions")]
#endif
        public string Instructions { get; set; }
    }

    public struct ServerCapabilities
    {
#if JSONNET_EXISTS
        [JsonProperty("tools")] 
#endif
        public object Tools { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("resources")]
#endif
        public object Resources { get; set; }
    }

    public struct ServerInfo
    {
#if JSONNET_EXISTS
        [JsonProperty("name")] 
#endif
        public string Name { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("version")] 
#endif
        public string Version { get; set; }
    }

    public struct ListToolsResult
    {
#if JSONNET_EXISTS
        [JsonProperty("tools")] 
#endif
        public List<ToolDescription> Tools { get; set; }
    }

    public struct ToolDescription
    {
#if JSONNET_EXISTS
        [JsonProperty("name")] 
#endif
        public string Name { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("description")] 
#endif
        public string Description { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("inputSchema")] 
#endif
        public InputSchema InputSchema { get; set; }
    }

    public struct ListResourcesResult
    {
#if JSONNET_EXISTS
        [JsonProperty("resources")]
#endif
        public List<ResourceDescription> Resources { get; set; }
    }

    public struct ResourceDescription
    {
#if JSONNET_EXISTS
        [JsonProperty("uri")]
#endif
        public string Uri { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("name")]
#endif
        public string Name { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("description")]
#endif
        public string Description { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("mimeType")]
#endif
        public string MimeType { get; set; }
    }

    [Serializable]
    public struct InputSchema
    {
#if JSONNET_EXISTS
        [JsonProperty("type")] 
#endif
        public string Type;
#if JSONNET_EXISTS
        [JsonProperty("properties")] 
#endif
        public SerializedDictionary<string, PropertySchema> Properties;
#if JSONNET_EXISTS
        [JsonProperty("required")] 
#endif
        public string[] Required;
    }

    [Serializable]
    public struct PropertySchema
    {
#if JSONNET_EXISTS
        [JsonProperty("type")]
#endif
        public string Type;
        [TextArea(1, 3)]
#if JSONNET_EXISTS
        [JsonProperty("description")]
#endif
        public string Description;
#if JSONNET_EXISTS
        [JsonProperty("items")]
#endif
        public ItemSchema Items;

        public bool ShouldSerializeDescription() => !string.IsNullOrEmpty(Description);
        public bool ShouldSerializeItems() => !string.IsNullOrEmpty(Items.Type);
    }

    [Serializable]
    public struct ItemSchema
    {
#if JSONNET_EXISTS
        [JsonProperty("type")] 
#endif
        public string Type;
        [TextArea(1, 3)]
#if JSONNET_EXISTS
        [JsonProperty("description")]
#endif
        public string Description;

        public bool ShouldSerializeDescription() => !string.IsNullOrEmpty(Description);
    }

    public struct CallToolParams
    {
#if JSONNET_EXISTS
        [JsonProperty("name")] 
#endif
        public string Name { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("arguments")] 
#endif
        public Dictionary<string, object> Arguments { get; set; }
    }

    public struct CallToolResult
    {
#if JSONNET_EXISTS
        [JsonProperty("content")] 
#endif
        public List<ContentItem> Content { get; set; }
    }

    public struct ReadResourceParams
    {
#if JSONNET_EXISTS
        [JsonProperty("uri")]
#endif
        public string Uri { get; set; }
    }

    public struct ReadResourceResult
    {
#if JSONNET_EXISTS
        [JsonProperty("contents")]
#endif
        public List<ResourceContentItem> Contents { get; set; }
    }

    public struct ContentItem
    {
#if JSONNET_EXISTS
        [JsonProperty("type")] 
#endif
        public string Type { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("text")] 
#endif
        public string Text { get; set; }
    }

    public struct ResourceContentItem
    {
#if JSONNET_EXISTS
        [JsonProperty("uri")]
#endif
        public string Uri { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("mimeType")]
#endif
        public string MimeType { get; set; }
#if JSONNET_EXISTS
        [JsonProperty("text")]
#endif
        public string Text { get; set; }
    }
}
