using Oxide.Ext.AdminPanel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Oxide.Ext.AdminPanel
{
    public class WSServer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyDictionary<string, IWebSocketDataProvider> _dataProviders;
        private readonly Timer _broadcastTimer;
        private readonly WebSocketSharp.Server.WebSocketServer _server;
        private readonly ConcurrentDictionary<WebSocketBehavior, bool> _connections = new();

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
                var result = new Dictionary<string, object>();
#if DEBUG
                _logger.LogInfo($"Total data providers: {_dataProviders.Count}");
#endif

                foreach (var provider in _dataProviders.Values)
                {
                    try
                    {
#if DEBUG
                        _logger.LogInfo($"Processing provider: {provider.GetType().Name}, DataKey: {provider.DataKey}");
#endif
                        var data = provider.GetWebSocketData();
                        if (data != null && data.Any())
                        {
#if DEBUG
                            _logger.LogInfo($"Data received from provider {provider.GetType().Name}: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");
#endif
                            result[provider.DataKey] = data;
                        }
                        else
                        {
#if DEBUG
                            _logger.LogWarning($"No data received from provider {provider.GetType().Name}");
#endif
                        }
                    }
#if DEBUG
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error getting data from provider {provider.GetType().Name}: {ex.Message}");
                    }
#else
                    catch
                    {
                        // do nothing
                    }
#endif

                }

                if (result.Any())
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(result);

                    foreach (var connection in _connections.Keys.ToList())
                    {
                        if (connection.Context?.WebSocket?.ReadyState == WebSocketState.Open)
                        {
#if DEBUG
                            _logger.LogInfo($"Broadcasting JSON: {json}");
#endif
                            connection.Context.WebSocket.Send(json);
                        }
                    }
                }
                else
                {
#if DEBUG
                    _logger.LogWarning("No data to broadcast.");
#endif
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
#if DEBUG
                    _owner._logger.LogInfo("Client Connected: " + Context.UserEndPoint);
#endif
                    _owner._connections.TryAdd(this, true);
                }
            }

            protected override void OnClose(CloseEventArgs e)
            {
                try
                {
                    if (Context != null && Context.UserEndPoint != null)
                    {
#if DEBUG
                        _owner._logger.LogInfo("Client Disconnected: " + Context.UserEndPoint);
#endif
                    }
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
                finally
                {
                    _owner._connections.TryRemove(this, out _);
                }
            }

            protected override void OnMessage(MessageEventArgs e)
            {
#if DEBUG
                _owner._logger.LogInfo("Message received: " + e.Data);
#endif

                Send("Server received: " + e.Data);
            }
        }
    }
}
