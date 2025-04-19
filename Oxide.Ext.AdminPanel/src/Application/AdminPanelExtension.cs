using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using Oxide.Core;
using Oxide.Core.Extensions;
using UnityEngine;
using VLB;

namespace Oxide.Ext.AdminPanel
{
    public class AdminPanelExtension : Extension, IDisposable
    {
        public override string Name => "ServerPanel";
        public override string Author => "LehaSex";
        internal static VersionNumber ExtensionVersion;

        private readonly IWebServer _webServer;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IDependencyContainer _container;
        private PathSettings _pathSettings = new PathSettings();
        private readonly AdminPanelConfig _config;

        public override VersionNumber Version => ExtensionVersion;

        public AdminPanelExtension(ExtensionManager manager) : base(manager)
        {
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            ExtensionVersion = new VersionNumber(assembly.Version.Major, assembly.Version.Minor, assembly.Version.Build);

            _logger = new OxideLogger() ?? throw new InvalidOperationException("Logger initialization failed");
            _fileSystem = new FileSystem() ?? throw new InvalidOperationException("FileSystem initialization failed");
            _container = new DependencyContainer();

            string configPath = Path.Combine(AppContext.BaseDirectory, "adminpanel_config.json");
            _config = AdminPanelConfig.Load(configPath);
            ValidateJwtKey();

            InitializePaths();
            InitDirectories();
            RegisterDependencies();

            RequestHandler requestHandler = _container.Resolve<RequestHandler>();
            _webServer = new WebServer(requestHandler, _logger, _container.Resolve<WSServer>());
        }

        private void InitializePaths()
        {
            _pathSettings.WwwRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            _pathSettings.Css = Path.Combine(_pathSettings.WwwRoot, "css");
            _pathSettings.Js = Path.Combine(_pathSettings.WwwRoot, "js");
            _pathSettings.Html = Path.Combine(_pathSettings.WwwRoot, "html");
        }

        public override async void OnModLoad()
        {
            try
            {
                if (_config.RequireHttps)
                {
                    _logger.LogInfo("HTTPS is required for AdminPanel");
                }

                await ExtractResources();
                _logger.LogInfo("AdminPanel extension loaded. Starting web server...");
                await _webServer.StartAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is SecurityException)
            {
                _logger.LogError($"SECURITY ERROR: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load AdminPanel: {ex}");
                throw;
            }
        }

        public override async void OnShutdown()
        {
            try
            {
                _logger.LogInfo("Stopping web server...");
                await _webServer.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during shutdown: {ex}");
            }
            finally
            {
                Dispose();
            }
        }

        private void InitDirectories()
        {
            var directories = new[] { _pathSettings.WwwRoot, _pathSettings.Css, _pathSettings.Js, _pathSettings.Html };

            foreach (var directory in directories)
            {
                try
                {
                    if (!_fileSystem.DirectoryExists(directory))
                    {
                        _fileSystem.CreateDirectory(directory);
                        _logger.LogInfo($"Directory created: {directory}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create directory {directory}: {ex}");
                    throw;
                }
            }
        }

        private async Task ExtractResources()
        {
            var resources = new Dictionary<string, string>
            {
                { "Oxide.Ext.AdminPanel.Resources.styles.css", Path.Combine(_pathSettings.Css, "styles.css") },
                { "Oxide.Ext.AdminPanel.Resources.scripts.js", Path.Combine(_pathSettings.Js, "scripts.js") },
                { "Oxide.Ext.AdminPanel.Resources.index.html", Path.Combine(_pathSettings.Html, "index.html") }
            };

            foreach (var resource in resources)
            {
                try
                {
                    await ResourceHelper.ExtractEmbeddedResourceAsync(resource.Key, resource.Value);
                    _logger.LogInfo($"File extracted: {resource.Value}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error extracting {resource.Key}: {ex}");
                    throw;
                }
            }
        }

        private void RegisterDependencies()
        {
            // Core dependencies
            _container.Register<IDependencyContainer>(() => _container);
            _container.Register<ILogger>(() => _logger);
            _container.Register<IFileSystem>(() => _fileSystem);

            RegisterMiddlewares();
            RegisterResponseHelper();
            RegisterRequestHandler();
            RegisterAuthComponents();
            RegisterControllers();
            RegisterWebSocketComponents();
/*            ValidateWebSocketDependencies();*/
        }

        private void RegisterResponseHelper()
        {
            _container.Register<IResponseHelper>(() =>
                new ResponseHelper(_logger, _fileSystem));
        }

        private void RegisterRequestHandler()
        {
            _container.Register<RequestHandler>(() =>
                new RequestHandler(_fileSystem, _logger, _container,
                    _pathSettings.WwwRoot, _pathSettings.Css,
                    _pathSettings.Js, _pathSettings.Html));
        }

        private void RegisterAuthComponents()
        {
            _container.Register<AuthController>(() =>
                new AuthController(_fileSystem, _pathSettings.Html,
                    _container.Resolve<IResponseHelper>(), _logger, _config.JwtSecretKey));
        }

        private void RegisterControllers()
        {
            _container.Register<Controller>(() =>
                new Controller(_fileSystem, _pathSettings.Html,
                    _container.Resolve<IResponseHelper>()));

            _container.Register<ApiController>(() =>
                new ApiController(_container.Resolve<Controller>()));

            _container.Register<ApiGetPlayerCount>(() =>
                new ApiGetPlayerCount(_container.Resolve<Controller>()));

            _container.Register<ApiGetPerformance>(() =>
                new ApiGetPerformance(_container.Resolve<Controller>()));
        }

        private void RegisterWebSocketComponents()
        {
            _container.Register<IWebSocketDataProvider>("performance", () =>
                new ApiGetPerformance(_container.Resolve<Controller>()));

            _container.Register<IWebSocketDataProvider>("players", () =>
                new ApiGetPlayerCount(_container.Resolve<Controller>()));

            _container.Register<WSServer>(() =>
            {
                var providers = new Dictionary<string, IWebSocketDataProvider>
                {
                    ["performance"] = _container.Resolve<IWebSocketDataProvider>("performance"),
                    ["players"] = _container.Resolve<IWebSocketDataProvider>("players")
                };
                return new WSServer(_logger, providers, "ws://0.0.0.0:8181");
            });

        }

        private void RegisterMiddlewares()
        {
            _container.Register<LoggingMiddleware>(() =>
                new LoggingMiddleware(_container.Resolve<ILogger>()));
            _container.Register<JwtAuthMiddleware>(() =>
                new JwtAuthMiddleware(_logger, _config.JwtSecretKey));
        }

/*        private void ValidateWebSocketDependencies()
        {
            try
            {
                _logger.LogInfo("Validating WebSocket dependencies...");

                var controller = _container.Resolve<Controller>();
                var provider = _container.Resolve<IWebSocketDataProvider>("performance");
                var handler = _container.Resolve<WebSocketHandler>();

                _logger.LogInfo("WebSocket dependencies validated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"WebSocket dependency validation failed: {ex}");
                throw;
            }
        }*/

        private void ValidateJwtKey()
        {
            if (string.IsNullOrWhiteSpace(_config.JwtSecretKey))
            {
                _logger.LogError("JWT secret key is empty!");
                throw new InvalidOperationException("JWT secret key must not be empty");
            }

            if (_config.JwtSecretKey.Length < 32)
            {
                _logger.LogWarning("JWT secret key is too short (recommended minimum is 32 chars)");
            }

            if (_config.JwtSecretKey == "123" || _config.JwtSecretKey == "secret")
            {
                _logger.LogError("INSECURE DEFAULT JWT SECRET KEY DETECTED!");
                throw new InvalidOperationException("Insecure default JWT key detected");
            }
        }

        public void Dispose()
        {
            (_webServer as IDisposable)?.Dispose();
            (_container as IDisposable)?.Dispose();
        }

        private class PathSettings
        {
            public string WwwRoot { get; set; } = string.Empty;
            public string Css { get; set; } = string.Empty;
            public string Js { get; set; } = string.Empty;
            public string Html { get; set; } = string.Empty;
        }
    }
}