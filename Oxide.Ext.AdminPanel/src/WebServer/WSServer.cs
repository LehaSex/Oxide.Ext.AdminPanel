using Oxide.Ext.AdminPanel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;

#nullable disable

namespace Oxide.Ext.AdminPanel
{
    public class WSServer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyDictionary<string, IWebSocketDataProvider> _dataProviders;
        private readonly Timer _broadcastTimer;
        private readonly WebSocketSharp.Server.WebSocketServer _server;
        private readonly ConcurrentBag<WebSocketBehavior> _connections = new();

        public WSServer(ILogger logger, IReadOnlyDictionary<string, IWebSocketDataProvider> dataProviders, string url)
        {
            _logger = logger;
            _dataProviders = dataProviders;

            _server = new WebSocketSharp.Server.WebSocketServer(url);
            _server.AddWebSocketService<AdminPanelBehavior>("/ws", () => new AdminPanelBehavior(this));

            _server.Start();
            _logger.LogInfo("WebSocket started at " + url);

            _broadcastTimer = new Timer(1000);
            _broadcastTimer.Elapsed += async (_, _) => await BroadcastAsync();
            _broadcastTimer.Start();
        }

        private async Task BroadcastAsync()
        {
            await Task.Run(() =>
            {
                foreach (var provider in _dataProviders.Values)
                {
                    var data = provider.GetWebSocketData();
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

                    foreach (var connection in _connections.ToList())
                    {
                        if (connection.Context?.WebSocket?.ReadyState == WebSocketState.Open)
                        {
                            connection.Context.WebSocket.Send(json);
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            _broadcastTimer?.Dispose();
            _server?.Stop();
        }

        private class AdminPanelBehavior : WebSocketBehavior
        {
            private readonly WSServer _owner;

            public AdminPanelBehavior(WSServer owner)
            {
                _owner = owner;
            }

            protected override void OnOpen()
            {
                if (Context != null)
                {
                    _owner._logger.LogInfo("Client Connected: " + Context.UserEndPoint);
                    _owner._connections.Add(this);
                }
            }

            protected override void OnClose(CloseEventArgs e)
            {
                if (Context != null)
                {
                    _owner._logger.LogInfo("Client Disconnected: " + Context.UserEndPoint);
                }
                _owner._connections.TryTake(out _);
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                _owner._logger.LogInfo("Message received: " + e.Data);

                Send("Server received: " + e.Data);
            }
        }
    }
}