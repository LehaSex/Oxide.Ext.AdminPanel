using Fleck;
using Oxide.Ext.AdminPanel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class WebSocketServer : IDisposable
    {
        private readonly Fleck.WebSocketServer _server;
        private readonly ConcurrentDictionary<IWebSocketConnection, Guid> _connectedClients = new();
        private readonly ILogger _logger;
        private readonly IReadOnlyDictionary<string, IWebSocketDataProvider> _dataProviders;
        private readonly System.Timers.Timer _broadcastTimer;

        public WebSocketServer(ILogger logger, IReadOnlyDictionary<string, IWebSocketDataProvider> dataProviders, string url)
        {
            _logger = logger;
            _dataProviders = dataProviders;

            _server = new Fleck.WebSocketServer(url);
            _server.Start(socket =>
            {
                socket.OnOpen = () => OnOpen(socket);
                socket.OnClose = () => OnClose(socket);
                socket.OnMessage = message => OnMessage(socket, message);
            });

            _broadcastTimer = new System.Timers.Timer(1000);
            _broadcastTimer.Elapsed += async (sender, e) => await BroadcastDataAsync();
            _broadcastTimer.Start();
        }

        private void OnOpen(IWebSocketConnection socket)
        {
            _connectedClients.TryAdd(socket, Guid.NewGuid());
            _logger.LogInfo($"WebSocket connection established. Client ID: {_connectedClients[socket]}");
        }

        private void OnClose(IWebSocketConnection socket)
        {
            if (_connectedClients.TryRemove(socket, out _))
            {
                _logger.LogInfo($"WebSocket connection closed.");
            }
        }

        private void OnMessage(IWebSocketConnection socket, string message)
        {
            _logger.LogInfo($"Received message from client: {message}");
        }

        private async Task BroadcastDataAsync()
        {
            if (_connectedClients.IsEmpty)
                return;

            try
            {
                var allData = new Dictionary<string, Dictionary<string, object>>();

                foreach (var provider in _dataProviders.Values)
                {
                    try
                    {
                        allData[provider.DataKey] = provider.GetWebSocketData();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Data provider {provider.DataKey} error: {ex}");
                    }
                }

                if (allData.Count == 0)
                    return;

                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(allData);

                foreach (var client in _connectedClients.Keys)
                {
                    try
                    {
                        await client.Send(jsonData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error sending to client: {ex}");
                        _connectedClients.TryRemove(client, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Broadcast error: {ex}");
            }
        }

        public void Dispose()
        {
            _server.Dispose();
            _broadcastTimer?.Dispose();
        }
    }
}