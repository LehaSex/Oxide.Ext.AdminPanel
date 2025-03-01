using System;
using System.Collections.Generic;
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

        private string _wwwrootPath;
        private string _cssPath;
        private string _jsPath;

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

            _wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            _cssPath = Path.Combine(_wwwrootPath, "css");
            _jsPath = Path.Combine(_wwwrootPath, "js");

            InitDirectories();

            if (_fileSystem == null)
            {
                throw new InvalidOperationException("FileSystem is not initialized.");
            }

            if (_logger == null)
            {
                throw new InvalidOperationException("Logger is not initialized.");
            }            

            // Web server instance
            var requestHandler = new RequestHandler(_fileSystem, _logger, _wwwrootPath, _cssPath, _jsPath);
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
                _cssPath
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

            // resources to extract
            var resources = new Dictionary<string, string>
            {
                { "Oxide.Ext.AdminPanel.Resources.styles.css", cssFilePath },
                { "Oxide.Ext.AdminPanel.Resources.scripts.js", jsFilePath }
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