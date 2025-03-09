using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Oxide.Core;
using Oxide.Core.Extensions;
using UnityEngine;

namespace Oxide.Ext.AdminPanel
{

    public class AdminPanelExtension : Extension
    {
        public override string Name => "ServerPanel";
        public override string Author => "LehaSex";

        internal static VersionNumber ExtensionVersion;

        private readonly IWebServer _webServer;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IDependencyContainer _container;

        private string _wwwrootPath;
        private string _cssPath;
        private string _jsPath;
        private string _htmlPath;

        /// <summary>
        /// Version number used by oxide
        /// </summary>
        public override VersionNumber Version => ExtensionVersion;

        /// <summary>
        /// Constructor for the extension
        /// </summary>
        /// <param name="manager">Oxide extension manager</param>
        public AdminPanelExtension(ExtensionManager manager) : base(manager)
        {
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            ExtensionVersion = new VersionNumber(assembly.Version.Major, assembly.Version.Minor, assembly.Version.Build);

            _logger = new OxideLogger();
            _fileSystem = new FileSystem();
            _container = new DependencyContainer();


            _wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            _cssPath = Path.Combine(_wwwrootPath, "css");
            _jsPath = Path.Combine(_wwwrootPath, "js");
            _htmlPath = Path.Combine(_wwwrootPath, "html");

            InitDirectories();

            if (_fileSystem == null)
            {
                throw new InvalidOperationException("FileSystem is not initialized.");
            }

            if (_logger == null)
            {
                throw new InvalidOperationException("Logger is not initialized.");
            }

            RegisterDependencies();

            // Web server instance
            var requestHandler = _container.Resolve<RequestHandler>();
            _webServer = new WebServer(requestHandler, _logger);
        }

        /// <summary>
        /// Called when mod is loaded
        /// </summary>
        public override async void OnModLoad()
        {
            await ExtractResources();
            _logger.LogInfo("AdminPanel extension loaded. Starting web server...");
            await _webServer.StartAsync();
        }

        /// <summary>
        /// Called when server is shutdown
        /// </summary>
        public override async void OnShutdown()
        {
            _logger.LogInfo("AdminPanel extension unloaded. Stopping web server...");
            await _webServer.StopAsync();
        }

        public void InitDirectories()
        {
            string[] directories = new[]
            {
                _wwwrootPath,
                _jsPath,
                _cssPath,
                _htmlPath,
            };

            foreach (string directory in directories)
            {
                if (!_fileSystem.DirectoryExists(directory))
                {
                    _fileSystem.CreateDirectory(directory);
                    _logger.LogInfo($"Directory created: {directory}");
                }
            }
        }

        private async Task ExtractResources()
        {
            // paths for saving files
            string cssFilePath = Path.Combine(_cssPath, "styles.css");
            string jsFilePath = Path.Combine(_jsPath, "scripts.js");
            string htmlFilePath = Path.Combine(_htmlPath, "index.html");

            // resources to extract
            var resources = new Dictionary<string, string>
            {
                { "Oxide.Ext.AdminPanel.Resources.styles.css", cssFilePath },
                { "Oxide.Ext.AdminPanel.Resources.scripts.js", jsFilePath },
                { "Oxide.Ext.AdminPanel.Resources.index.html", htmlFilePath }
            };

            foreach (var resource in resources)
            {
                string resourceName = resource.Key;
                string outputPath = resource.Value;

                try
                {
                    await ResourceHelper.ExtractEmbeddedResourceAsync(resourceName, outputPath);
                    _logger.LogInfo($"File '{Path.GetFileName(outputPath)}' extracted and saved: {outputPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error extracting '{Path.GetFileName(outputPath)}': {ex}");
                }
            }
        }

        private void RegisterDependencies()
        {
            _container.Register<IDependencyContainer>(() => _container);
            _container.Register<ILogger, OxideLogger>();
            _container.Register<IFileSystem, FileSystem>();
            _container.Register<IResponseHelper>(() =>
            {
                return new ResponseHelper(
                    _container.Resolve<ILogger>(),
                    _container.Resolve<IFileSystem>()
                );
            });
            // request handler factory
            _container.Register<RequestHandler>(() =>
            {
                var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                var cssPath = Path.Combine(wwwrootPath, "css");
                var jsPath = Path.Combine(wwwrootPath, "js");
                var htmlPath = Path.Combine(wwwrootPath, "html");

                return new RequestHandler(
                    _container.Resolve<IFileSystem>(),
                    _container.Resolve<ILogger>(),
                    _container,
                    wwwrootPath,
                    cssPath,
                    jsPath,
                    htmlPath
                );
            });

            // auth controller factory
            _container.Register<AuthController>(() =>
            {
                var fileSystem = _container.Resolve<IFileSystem>();
                var responseHelper = _container.Resolve<IResponseHelper>();
                string htmlPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "html");
                string secretKey = "123";

                return new AuthController(_container.Resolve<IFileSystem>(), htmlPath, _container.Resolve<IResponseHelper>(), _container.Resolve<ILogger>(), secretKey);
            });

            // main panel controller factory
            _container.Register<MainPanelController>(() =>
            {
                var fileSystem = _container.Resolve<IFileSystem>();
                var responseHelper = _container.Resolve<IResponseHelper>();
                string htmlPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "html");

                return new MainPanelController(fileSystem, htmlPath, responseHelper);
            });

            // MIDDLEWARE
            _container.Register<LoggingMiddleware, LoggingMiddleware>();

            _container.Register<JwtAuthMiddleware>(() => new JwtAuthMiddleware(
                _container.Resolve<ILogger>(),
            "123"
            ));

            _container.Register<Controller>(() => new Controller(
                _container.Resolve<IFileSystem>(),
                "wwwroot/html", // Путь к HTML-файлам
                _container.Resolve<IResponseHelper>()
            ));

            _container.Register<ApiController>(() => new ApiController(
                _container.Resolve<Controller>()
            ));

            _container.Register<ApiGetPlayerCount>(() => new ApiGetPlayerCount(
                _container.Resolve<Controller>() 
            ));            

            _container.Register<ApiGetPerformance>(() => new ApiGetPerformance(
                _container.Resolve<Controller>() 
            ));

        }

        /// <summary>
        /// Get path to wwwroot
        /// </summary>
        public string GetWwwRootPath()
        {
            return _wwwrootPath;
        }        
        
        /// <summary>
        /// Get path to wwwroot
        /// </summary>
        public string GetCSSPath()
        {
            return _cssPath;
        }        
        /// <summary>
        /// Get path to wwwroot
        /// </summary>
        public string GetJSPath()
        {
            return _jsPath;
        }

    } 
}