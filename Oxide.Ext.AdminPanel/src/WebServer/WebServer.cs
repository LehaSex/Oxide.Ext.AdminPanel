using Oxide.Core;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class WebServer : IWebServer
    {
        private readonly HttpListener _httpListener;
        private readonly RequestHandler _requestHandler;
        private readonly ILogger _logger;
        private Task? _serverTask;
        private CancellationTokenSource _cts;

        public WebServer(RequestHandler requestHandler, ILogger logger)
        {
            _httpListener = new HttpListener();
            _requestHandler = requestHandler;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }

        public Task StartAsync()
        {
            _httpListener.Prefixes.Add("http://localhost/adminpanel/");
            _httpListener.Start();

            _serverTask = ListenForRequestsAsync(_cts.Token);
            _logger.LogInfo("Web server started at http://localhost/adminpanel/");
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            _httpListener.Stop();
            if (_serverTask != null) 
            {
                await _serverTask;
            }

            _logger.LogInfo("Web server stopped.");
        }

        private async Task ListenForRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = await _httpListener.GetContextAsync();
                    await _requestHandler.ProcessRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing request: {ex}");
                }
            }
        }

        public void Dispose()
        {
            _httpListener.Close();
        }
    }
}