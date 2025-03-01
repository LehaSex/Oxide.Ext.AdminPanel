using Oxide.Core;
using System;
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

        public RequestHandler(IFileSystem fileSystem, ILogger logger, string wwwrootPath, string cssPath, string jsPath)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wwwrootPath = wwwrootPath ?? throw new ArgumentNullException(nameof(wwwrootPath));
            _cssPath = cssPath ?? throw new ArgumentNullException(nameof(cssPath));
            _jsPath = jsPath ?? throw new ArgumentNullException(nameof(jsPath));
        }

        public async Task ProcessRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            _logger.LogInfo($"Processing request: {request.Url.AbsolutePath}");

            try
            {
                // static file processing
                if (request.Url.AbsolutePath.StartsWith("/adminpanel/css/", StringComparison.OrdinalIgnoreCase) ||
                    request.Url.AbsolutePath.StartsWith("/adminpanel/js/", StringComparison.OrdinalIgnoreCase))
                {
                    await ServeStaticFileAsync(request, response);
                    return;
                }

                // processing main request (HTML-page)
                if (request.Url.AbsolutePath == "/" ||
                    request.Url.AbsolutePath == "/adminpanel" ||
                    request.Url.AbsolutePath == "/adminpanel/")
                {
                    await ServeHtmlPageAsync(response);
                    return;
                }

                // if not found return 404
                response.StatusCode = 404;
                await ServeContentAsync(response, Encoding.UTF8.GetBytes("Not Found"), "text/plain");
                _logger.LogError($"Unhandled request: {request.Url.AbsolutePath}");
            }
            catch (Exception ex)
            {
                // exceptions
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
            string htmlContent = @"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>AdminPanel</title>
                <link rel=""stylesheet"" href=""/adminpanel/css/styles.css"">
                <script src=""/adminpanel/js/scripts.js""></script>
            </head>
            <body class='bg-gray-100 flex items-center justify-center h-screen'>
                <div class='bg-white p-8 rounded-lg shadow-lg text-center'>
                    <h1 class='text-2xl font-bold text-gray-800 mb-4'>Welcome to AdminPanel!</h1>
                    <button class='bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600'>
                        Click Me
                    </button>
                </div>
            </body>
            </html>";

            _logger.LogInfo("Serving HTML page");
            await ServeContentAsync(response, Encoding.UTF8.GetBytes(htmlContent), "text/html");
        }

        private async Task ServeFileAsync(HttpListenerResponse response, string filePath, string contentType)
        {
            _logger.LogInfo($"Reading file: {filePath}");
            byte[] buffer = await _fileSystem.ReadFileAsync(filePath);
            _logger.LogInfo($"File read successfully: {filePath}");
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