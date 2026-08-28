using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if JSONNET_EXISTS
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace DA_Assets.Shared.MCP
{
    public sealed class WebSocketTransportClient : IDisposable
    {
        private readonly CommandRegistry _registry;
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private Task _receiveTask;
        private string _sessionId;
        private string _lastError;

        public WebSocketTransportClient(CommandRegistry registry)
        {
            _registry = registry;
        }

        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open && !string.IsNullOrEmpty(_sessionId);
        public bool IsActive => _socket != null && (_socket.State == WebSocketState.Connecting || _socket.State == WebSocketState.Open);
        public string SessionId => _sessionId;
        public string LastError => _lastError;

        public async Task StartAsync(string baseUrl)
        {
            await StopAsync();

            _registry.Rebuild();
            _cts = new CancellationTokenSource();
            _socket = new ClientWebSocket();
            Uri endpoint = BuildBridgeUri(baseUrl);

            await _socket.ConnectAsync(endpoint, _cts.Token);
            _lastError = null;
            _receiveTask = ReceiveLoopAsync(_socket, _cts, _cts.Token);
        }

        public async Task StopAsync()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }

            if (_socket != null)
            {
                try
                {
                    if (_socket.State == WebSocketState.Open)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopped", CancellationToken.None);
                    }
                }
                catch
                {
                }

                _socket.Dispose();
                _socket = null;
            }

            _cts?.Dispose();
            _cts = null;
            _receiveTask = null;
            _sessionId = null;
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationTokenSource cts, CancellationToken token)
        {
            var buffer = new byte[64 * 1024];

            try
            {
                while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    string json = await ReceiveMessageAsync(socket, buffer, token);
                    if (string.IsNullOrEmpty(json))
                    {
                        continue;
                    }

                    await HandleMessageAsync(json, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                Debug.LogWarning($"DA MCP bridge disconnected: {ex.Message}");
            }
            finally
            {
                if (ReferenceEquals(_socket, socket))
                {
                    socket.Dispose();
                    _socket = null;
                    _sessionId = null;
                    cts.Dispose();
                    _cts = null;
                    _receiveTask = null;
                }
            }
        }

        private static async Task<string> ReceiveMessageAsync(ClientWebSocket socket, byte[] buffer, CancellationToken token)
        {
            var builder = new StringBuilder();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return string.Empty;
                }

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            return builder.ToString();
        }

        private async Task HandleMessageAsync(string json, CancellationToken token)
        {
#if JSONNET_EXISTS
            JObject message = JObject.Parse(json);
            string type = message.Value<string>("type");

            switch (type)
            {
                case "welcome":
                    await SendRegisterAsync(token);
                    break;
                case "registered":
                    _sessionId = message.Value<string>("session_id");
                    await SendRegisterToolsAsync(token);
                    await SendRegisterResourcesAsync(token);
                    break;
                case "ping":
                    await SendJsonAsync(new { type = "pong", session_id = _sessionId }, token);
                    break;
                case "execute":
                    await HandleExecuteAsync(message, token);
                    break;
            }
#else
            await Task.CompletedTask;
#endif
        }

#if JSONNET_EXISTS
        private async Task HandleExecuteAsync(JObject message, CancellationToken token)
        {
            string id = message.Value<string>("id");
            string name = message.Value<string>("name");
            string endpointPath = message.Value<string>("endpoint_path") ?? "";
            Dictionary<string, object> args = ToDictionary(message["params"] as JObject);

            CommandResult result = await TransportCommandDispatcher.RunOnMainThreadAsync(
                () => _registry.ExecuteAsync(endpointPath, name, args));
            await SendJsonAsync(new
            {
                type = "command_result",
                id,
                result = new
                {
                    status = result.status,
                    content = result.content,
                    error = result.error
                }
            }, token);
        }

        private async Task SendRegisterAsync(CancellationToken token)
        {
            await SendJsonAsync(new
            {
                type = "register",
                project_name = ProjectIdentity.ProjectName,
                project_hash = ProjectIdentity.ProjectHash,
                unity_version = Application.unityVersion,
                project_path = ProjectIdentity.ProjectPath
            }, token);
        }

        private async Task SendRegisterToolsAsync(CancellationToken token)
        {
            await SendJsonAsync(new
            {
                type = "register_tools",
                tools = _registry.ToolDescriptors
            }, token);
        }

        private async Task SendRegisterResourcesAsync(CancellationToken token)
        {
            await SendJsonAsync(new
            {
                type = "register_resources",
                resources = _registry.ResourceDescriptors
            }, token);
        }

        private async Task SendJsonAsync(object payload, CancellationToken token)
        {
            string json = JsonConvert.SerializeObject(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }

        private static Dictionary<string, object> ToDictionary(JObject obj)
        {
            var result = new Dictionary<string, object>();
            if (obj == null)
            {
                return result;
            }

            foreach (JProperty property in obj.Properties())
            {
                result[property.Name] = ToPlainObject(property.Value);
            }

            return result;
        }

        private static object ToPlainObject(JToken token)
        {
            return token switch
            {
                JValue value => value.Value,
                JObject obj => ToDictionary(obj),
                JArray array => ToList(array),
                _ => token.ToString()
            };
        }

        private static List<object> ToList(JArray array)
        {
            var result = new List<object>();
            foreach (JToken item in array)
            {
                result.Add(ToPlainObject(item));
            }
            return result;
        }
#endif

        private static Uri BuildBridgeUri(string baseUrl)
        {
            Uri baseUri = new Uri(baseUrl);
            string scheme = baseUri.Scheme == "https" ? "wss" : "ws";
            var builder = new UriBuilder(baseUri)
            {
                Scheme = scheme,
                Path = "/hub/bridge"
            };
            return builder.Uri;
        }
    }
}
