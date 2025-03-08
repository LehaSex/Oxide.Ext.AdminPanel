using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class WebSocketServer
    {
        private readonly HttpListener _listener;
        private int _fps = 0; // Переменная, которую будем обновлять

        public WebSocketServer(string uri)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(uri);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("WebSocket server started.");

            while (true)
            {
                var context = await _listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    var webSocketContext = await context.AcceptWebSocketAsync(null);
                    await HandleWebSocketAsync(webSocketContext.WebSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {
                // Обновляем значение FPS (в реальном проекте это может быть внешний источник)
                //_fps = ServerPerformance.lastfps;

                // Отправляем значение FPS клиенту
                string message = $"FPS: {_fps}";
                var bytes = Encoding.UTF8.GetBytes(message);
                //await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                // Ждем 1 секунду перед следующей отправкой
                await Task.Delay(1000);
            }
        }
    }
}
