using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Oxide.Core;

namespace Oxide.Ext.AdminPanel
{
    public class WebSocketHandler : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyDictionary<string, IWebSocketDataProvider> _dataProviders;
        private readonly Timer _broadcastTimer;
        private readonly ConcurrentDictionary<Guid, WebSocketClient> _connectedClients = new();
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        public WebSocketHandler(ILogger logger, IReadOnlyDictionary<string, IWebSocketDataProvider> dataProviders)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataProviders = dataProviders ?? throw new ArgumentNullException(nameof(dataProviders));

            _broadcastTimer = new Timer(
                callback: _ => _ = BroadcastDataAsync(),
                state: null,
                dueTime: 1000,
                period: 1000);

            _logger.LogInfo($"WebSocketHandler created with {dataProviders.Count} data providers");
            foreach (var provider in dataProviders)
            {
                _logger.LogInfo($" - Provider: {provider.Key} ({provider.Value.GetType().Name})");
            }
        }

        private async Task BroadcastDataAsync()
        {
            if (_connectedClients.IsEmpty || _cts.IsCancellationRequested)
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
                var buffer = Encoding.UTF8.GetBytes(jsonData);

                foreach (var clientPair in _connectedClients)
                {
                    try
                    {
                        await SendToClientAsync(clientPair.Value, buffer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error sending to client {clientPair.Key}: {ex}");
                        _ = TryRemoveClient(clientPair.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Broadcast error: {ex}");
            }
        }

        private async Task SendToClientAsync(WebSocketClient client, byte[] buffer)
        {
            if (client.WebSocket.State != WebSocketState.Open)
            {
                _ = TryRemoveClient(client.Id);
                return;
            }

            await client.WebSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                _cts.Token);
        }

        private async Task<bool> TryRemoveClient(Guid clientId)
        {
            if (_connectedClients.TryRemove(clientId, out var client))
            {
                await CleanupClient(client);
                return true;
            }
            return false;
        }

        private async Task CleanupClient(WebSocketClient client)
        {
            try
            {
                if (client.WebSocket.State == WebSocketState.Open)
                {
                    await client.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None);
                }
                client.WebSocket.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up client {client.Id}: {ex}");
            }
        }

        public async Task HandleAsync(HttpListenerWebSocketContext wsContext)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WebSocketHandler));

            try
            {
                var client = new WebSocketClient(wsContext.WebSocket);

                if (!_connectedClients.TryAdd(client.Id, client))
                {
                    await CleanupClient(client);
                    return;
                }

                _logger.LogInfo($"WebSocket connection established. Client ID: {client.Id}");

                await ProcessClientMessagesAsync(client);
            }
            catch (Exception ex)
            {
                _logger.LogError($"WebSocket connection error: {ex}");
            }
        }

        private async Task ProcessClientMessagesAsync(WebSocketClient client)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (!_cts.IsCancellationRequested && client.WebSocket.State == WebSocketState.Open)
                {
                    var result = await client.WebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInfo($"Client {client.Id} initiated close");
                        await TryRemoveClient(client.Id);
                        return;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInfo($"Received from {client.Id}: {message}");
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError($"WebSocket client {client.Id} error: {ex}");
            }
            finally
            {
                await TryRemoveClient(client.Id);
            }
        }

        public async void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _broadcastTimer?.Dispose();
            _cts.Cancel();

            // clean up connections
            var cleanupTasks = new List<Task>();
            foreach (var clientId in _connectedClients.Keys)
            {
                cleanupTasks.Add(TryRemoveClient(clientId));
            }

            await Task.WhenAll(cleanupTasks);
            _cts.Dispose();
        }

        private class WebSocketClient
        {
            public Guid Id { get; } = Guid.NewGuid();
            public WebSocket WebSocket { get; }

            public WebSocketClient(WebSocket webSocket)
            {
                WebSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            }
        }
    }
}