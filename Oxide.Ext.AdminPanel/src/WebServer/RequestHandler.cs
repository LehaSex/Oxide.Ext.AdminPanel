using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Linq;

namespace Oxide.Ext.AdminPanel
{
    public sealed class RequestHandler : IDisposable
    {
        private static readonly Type[] EmptyMiddlewareTypes = Array.Empty<Type>();
        private sealed class RouteDefinition
        {
            public Type ControllerType { get; }
            public Type[] MiddlewareTypes { get; }
            public string MethodName { get; }

            public RouteDefinition(Type controllerType, Type[] middlewareTypes, string methodName = "HandleRequest")
            {
                ControllerType = controllerType;
                MiddlewareTypes = middlewareTypes;
                MethodName = methodName;
            }
        }

        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IDependencyContainer _container;
        private readonly StaticFileHandler _staticFileHandler;
        private readonly ConcurrentDictionary<string, RouteDefinition> _routes;
        private bool _disposed;


        public RequestHandler(
            IFileSystem fileSystem,
            ILogger logger,
            IDependencyContainer container,
            string wwwrootPath,
            string cssPath,
            string jsPath,
            string htmlPath)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _container = container ?? throw new ArgumentNullException(nameof(container));

            _staticFileHandler = new StaticFileHandler(fileSystem, wwwrootPath, cssPath, jsPath, htmlPath);
            _routes = new ConcurrentDictionary<string, RouteDefinition>(StringComparer.OrdinalIgnoreCase);

            RegisterCoreRoutes();
        }

        private void RegisterCoreRoutes()
        {

            RegisterRoute("/adminpanel/auth",
                typeof(AuthController),
                new[] { typeof(LoggingMiddleware) });

            RegisterRoute("/adminpanel/mainpanel",
                typeof(MainPanelController),
                new[] { typeof(LoggingMiddleware), typeof(JwtAuthMiddleware) });

            RegisterRoute("/adminpanel/api/player/count",
                typeof(ApiGetPlayerCount),
                new[] { typeof(LoggingMiddleware) },
                "GetPlayerCount");

            RegisterRoute("/adminpanel/api/server/performance",
                typeof(ApiGetPerformance),
                new[] { typeof(LoggingMiddleware) },
                "GetPerformance");
        }

        private void RegisterRoute(string path, Type controllerType, Type[] middlewareTypes, string methodName = "HandleRequest")
        {
            if (!_routes.TryAdd(path, new RouteDefinition(controllerType, middlewareTypes, methodName)))
            {
                throw new InvalidOperationException($"Route '{path}' is already registered");
            }
        }

        private void RegisterRoute(string path, Func<HttpListenerContext, Task> handler)
        {
            // only for WebSocket
            _routes.TryAdd(path, new RouteDefinition(typeof(object), EmptyMiddlewareTypes, "Dummy"));
        }

        public async Task ProcessRequestAsync(HttpListenerContext httpContext)
        {
            if (_disposed)
            {
                _logger.LogError("Attempted to use disposed RequestHandler");
                throw new ObjectDisposedException(nameof(RequestHandler));
            }

            var request = httpContext.Request;
            var response = httpContext.Response;
            var requestPath = request.Url.AbsolutePath;
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8); // short ID for logs

            try
            {
                LogRequestStart(requestId, request);

                var stopwatch = Stopwatch.StartNew();
                var handled = false;

                // request processing procedure
                if (TryHandleOptionsRequest(httpContext))
                {
                    handled = true;
                }

                else if (await TryHandleStaticFileAsync(request, response))
                {
                    handled = true;
                }
                else if (await TryHandleRouteAsync(httpContext))
                {
                    handled = true;
                }
                else if (await TryHandleDefaultPageAsync(request, response))
                {
                    handled = true;
                }

                stopwatch.Stop();

                if (!handled)
                {
                    _logger.LogWarning($"No handler found for {requestPath}");
                    await HandleNotFoundAsync(response);
                }
                else
                {
                    _logger.LogInfo($"Request {requestId} processed in {stopwatch.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Request {requestId} failed: {ex}");
                await HandleErrorAsync(response, ex);
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to close response for {requestId}: {ex}");
                }
            }
        }
        private bool TryHandleOptionsRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                context.Response.Close();
                return true;
            }
            return false;
        }

        private void LogRequestStart(string requestId, HttpListenerRequest request)
        {
            var logMessage = new StringBuilder()
                .Append($"Request {requestId} started: ")
                .Append($"Path={request.Url.AbsolutePath}, ")
                .Append($"Method={request.HttpMethod}, ")
                .Append($"UserAgent={request.UserAgent ?? "none"}, ")
                .Append($"ClientIP={request.RemoteEndPoint?.Address.ToString() ?? "unknown"}");

            _logger.LogInfo(logMessage.ToString());
        }

        private async Task HandleErrorAsync(HttpListenerResponse response, Exception ex)
        {
            try
            {
                response.StatusCode = 500;
                await response.WriteResponseAsync("Internal Server Error", "text/plain");
            }
            catch (Exception responseEx)
            {
                _logger.LogError($"Failed to send error response: {responseEx}");
            }
        }

        private async Task HandleNotFoundAsync(HttpListenerResponse response)
        {
            try
            {
                response.StatusCode = 404;
                await response.WriteResponseAsync("Not Found", "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send 404 response: {ex}");
            }
        }

        private async Task<bool> TryHandleStaticFileAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            return await _staticFileHandler.TryHandleRequestAsync(request, response);
        }

        private async Task<bool> TryHandleRouteAsync(HttpListenerContext httpContext)
        {
            if (!_routes.TryGetValue(httpContext.Request.Url.AbsolutePath, out var routeDef))
                return false;

            if (routeDef.ControllerType == null)
            {
                // Handle WebSocket request or return false
                return false;
            }

            var controller = _container.Resolve(routeDef.ControllerType);
            var method = routeDef.ControllerType.GetMethod(routeDef.MethodName)
                ?? throw new InvalidOperationException($"Method {routeDef.MethodName} not found");

            var handler = (Func<HttpListenerContext, Task>)Delegate.CreateDelegate(
                typeof(Func<HttpListenerContext, Task>),
                controller,
                method);

            await ExecuteWithMiddleware(httpContext, handler, routeDef.MiddlewareTypes);
            return true;
        }

        private async Task<bool> TryHandleDefaultPageAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!IsRootPath(request.Url.AbsolutePath))
                return false;

            await _staticFileHandler.ServeDefaultPageAsync(response);
            return true;
        }

        private bool IsRootPath(string path) =>
            path.Equals("/") || path.Equals("/adminpanel") || path.Equals("/adminpanel/");

        private async Task ExecuteWithMiddleware(
            HttpListenerContext context,
            Func<HttpListenerContext, Task> handler,
            Type[] middlewareTypes)
        {
            Func<Task> next = () => handler(context);

            for (int i = middlewareTypes.Length - 1; i >= 0; i--)
            {
                var middlewareType = middlewareTypes[i];
                try
                {
                    var middleware = _container.Resolve(middlewareType) as IMiddleware;
                    if (middleware == null)
                    {
                        _logger.LogWarning($"Middleware {middlewareType.Name} resolved as null");
                        continue;
                    }

                    var currentNext = next;
                    next = () => middleware.InvokeAsync(context, currentNext);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to resolve middleware {middlewareType.Name}: {ex}");
                    throw;
                }
            }

            await next();
        }



        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _routes.Clear();
        }
    }

    internal static class HttpListenerResponseExtensions
    {
        public static async Task WriteResponseAsync(this HttpListenerResponse response, string content, string contentType)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentType = contentType;
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    internal sealed class StaticFileHandler
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _defaultPagePath;
        private readonly Dictionary<string, string> _staticFileMap;

        public StaticFileHandler(
            IFileSystem fileSystem,
            string wwwrootPath,
            string cssPath,
            string jsPath,
            string htmlPath)
        {
            _fileSystem = fileSystem;
            _defaultPagePath = Path.Combine(htmlPath, "index.html");

            _staticFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["/adminpanel/css/styles.css"] = Path.Combine(cssPath, "styles.css"),
                ["/adminpanel/js/scripts.js"] = Path.Combine(jsPath, "scripts.js")
            };
        }

        public async Task<bool> TryHandleRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!_staticFileMap.TryGetValue(request.Url.AbsolutePath, out var filePath))
                return false;

            if (!await _fileSystem.FileExistsAsync(filePath))
            {
                await response.WriteResponseAsync("File not found", "text/plain");
                return true;
            }

            await ServeFileAsync(response, filePath);
            return true;
        }

        public async Task ServeDefaultPageAsync(HttpListenerResponse response)
        {
            if (!await _fileSystem.FileExistsAsync(_defaultPagePath))
            {
                await response.WriteResponseAsync("Default page not found", "text/plain");
                return;
            }

            await ServeFileAsync(response, _defaultPagePath);
        }

        private async Task ServeFileAsync(HttpListenerResponse response, string filePath)
        {
            var content = await _fileSystem.ReadFileAsync(filePath);
            var contentType = GetContentType(filePath);
            response.ContentType = contentType;
            response.ContentLength64 = content.Length;
            await response.OutputStream.WriteAsync(content, 0, content.Length);
            response.OutputStream.Close();
        }

        private string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".html" => "text/html",
                _ => "application/octet-stream"
            };
        }
    }
}