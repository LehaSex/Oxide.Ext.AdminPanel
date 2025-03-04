using Oxide.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class RequestHandler
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly string _wwwrootPath;
        private readonly string _cssPath;
        private readonly string _jsPath;
        private readonly string _htmlPath;
        private readonly Dictionary<string, Func<HttpListenerContext, Task>> _routes;
        private readonly IDependencyContainer _container;

        public RequestHandler(IFileSystem fileSystem, ILogger logger, IDependencyContainer container, string wwwrootPath, string cssPath, string jsPath, string htmlPath)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wwwrootPath = wwwrootPath ?? throw new ArgumentNullException(nameof(wwwrootPath));
            _cssPath = cssPath ?? throw new ArgumentNullException(nameof(cssPath));
            _jsPath = jsPath ?? throw new ArgumentNullException(nameof(jsPath));
            _htmlPath = htmlPath ?? throw new ArgumentNullException(nameof(htmlPath));
            _routes = new Dictionary<string, Func<HttpListenerContext, Task>>();
            _container = container ?? throw new ArgumentNullException(nameof(container));

            // Регистрируем маршруты
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            // Разрешаем контроллеры через DI-контейнер
            var authController = _container.Resolve<AuthController>();
            var mainController = _container.Resolve<MainPanelController>();

            _routes["/adminpanel/auth"] = context => ExecuteWithMiddleware(context, authController.HandleRequest, typeof(LoggingMiddleware));
            _routes["/adminpanel/mainpanel"] = context => ExecuteWithMiddleware(context, mainController.HandleRequest, typeof(LoggingMiddleware));
        }

        private async Task ExecuteWithMiddleware(HttpListenerContext context, Func<HttpListenerContext, Task> handler, params Type[] middlewareTypes)
        {
            Func<Task> next = () => handler(context);

            // Создаем и выполняем middleware в обратном порядке
            for (int i = middlewareTypes.Length - 1; i >= 0; i--)
            {
                var middleware = _container.Resolve(middlewareTypes[i]) as IMiddleware;

                // Пропускаем middleware, если его не удалось создать
                if (middleware == null)
                {
                    var logger = _container.Resolve<ILogger>();
                    logger.LogWarning($"Failed to create middleware of type {middlewareTypes[i]}");
                    continue;
                }

                var nextCopy = next;
                next = () => middleware.InvokeAsync(context, nextCopy);
            }

            await next();
        }

        public async Task ProcessRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            _logger.LogInfo($"Processing request: {request.Url.AbsolutePath}");

            try
            {
                // Обработка статических файлов (CSS, JS)
                if (request.Url.AbsolutePath.StartsWith("/adminpanel/css/", StringComparison.OrdinalIgnoreCase) ||
                    request.Url.AbsolutePath.StartsWith("/adminpanel/js/", StringComparison.OrdinalIgnoreCase))
                {
                    await ServeStaticFileAsync(request, response);
                    return;
                }

                // Обработка маршрутов
                if (_routes.TryGetValue(request.Url.AbsolutePath, out var handler))
                {
                    await handler(context);
                    return;
                }

                // Обработка HTML-страниц
                if (request.Url.AbsolutePath == "/" ||
                    request.Url.AbsolutePath == "/adminpanel" ||
                    request.Url.AbsolutePath == "/adminpanel/")
                {
                    await ServeHtmlPageAsync(response);
                    return;
                }

                // Если ничего не найдено, возвращаем 404
                response.StatusCode = 404;
                await ServeContentAsync(response, Encoding.UTF8.GetBytes("Not Found"), "text/plain");
                _logger.LogError($"Unhandled request: {request.Url.AbsolutePath}");
            }
            catch (Exception ex)
            {
                // Обработка исключений
                response.StatusCode = 500;
                await ServeContentAsync(response, Encoding.UTF8.GetBytes("Internal Server Error"), "text/plain");
                _logger.LogError($"Error processing request: {ex}");
            }
        }

        private async Task ServeStaticFileAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            _logger.LogInfo($"Serving Static File: {request.Url.AbsolutePath}");
            string filePath = request.Url.AbsolutePath switch
            {
                "/adminpanel/css/styles.css" => Path.Combine(_cssPath, "styles.css"),
                "/adminpanel/js/scripts.js" => Path.Combine(_jsPath, "scripts.js"),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(filePath) && await _fileSystem.FileExistsAsync(filePath))
            {
                _logger.LogInfo($"Serving static file: {filePath}");
                await ServeFileAsync(response, filePath, GetContentType(filePath));
            }
            else
            {
                _logger.LogError($"File not found: {request.Url.AbsolutePath}");
                response.StatusCode = 404;
                await ServeContentAsync(response, Encoding.UTF8.GetBytes("File not found"), "text/plain");
            }
        }

        private async Task ServeHtmlPageAsync(HttpListenerResponse response)
        {
            string htmlFilePath = Path.Combine(_htmlPath, "index.html");

            if (!await _fileSystem.FileExistsAsync(htmlFilePath))
            {
                _logger.LogError($"HTML file not found: {htmlFilePath}");
                response.StatusCode = 404;
                await ServeContentAsync(response, Encoding.UTF8.GetBytes("HTML file not found"), "text/plain");
                return;
            }

            _logger.LogInfo($"Serving HTML page from: {htmlFilePath}");
            await ServeFileAsync(response, htmlFilePath, "text/html");
        }

        private async Task ServeFileAsync(HttpListenerResponse response, string filePath, string contentType)
        {
            byte[] buffer = await _fileSystem.ReadFileAsync(filePath);
            await ServeContentAsync(response, buffer, contentType);
        }

        private async Task ServeContentAsync(HttpListenerResponse response, byte[] buffer, string contentType)
        {
            response.ContentLength64 = buffer.Length;
            response.ContentType = contentType;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            await response.OutputStream.FlushAsync();
            response.OutputStream.Close();
        }

        private string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath) switch
            {
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".html" => "text/html",
                _ => "text/plain"
            };
        }
    }
}