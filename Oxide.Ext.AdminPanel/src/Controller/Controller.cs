using Oxide.Ext.AdminPanel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;

namespace Oxide.Ext.AdminPanel
{
    public class Controller
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _htmlPath;
        private readonly ResponseHelper _responseHelper;

        /// <summary>
        /// Controller, logic
        /// </summary>
        /// <param name="fileSystem">file system</param>
        /// <param name="htmlPath">name of file in wwwroot/html/</param>
        /// <param name="responseHelper">response helper</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Controller(IFileSystem fileSystem, string htmlPath, ResponseHelper responseHelper)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _htmlPath = htmlPath ?? throw new ArgumentNullException(nameof(htmlPath));
            _responseHelper = responseHelper ?? throw new ArgumentNullException(nameof(responseHelper));
        }

        protected async Task View(HttpListenerResponse response, string viewName)
        {
            string viewPath = Path.Combine(_htmlPath, $"{viewName}.html");

            if (!await _fileSystem.FileExistsAsync(viewPath))
            {
                response.StatusCode = 404;
                await _responseHelper.ServeContentAsync(response, Encoding.UTF8.GetBytes("View not found"), "text/plain");
                return;
            }

            await _responseHelper.ServeFileAsync(response, viewPath, "text/html");
        }
    }
}