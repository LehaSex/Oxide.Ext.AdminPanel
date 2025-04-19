using Oxide.Ext.AdminPanel;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Collections.Concurrent;


namespace Oxide.Ext.AdminPanel
{
    public class ResponseHelper : IResponseHelper
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ConcurrentDictionary<string, byte[]> _fileCache = new ConcurrentDictionary<string, byte[]>(); // file cache

        public ResponseHelper(ILogger logger, IFileSystem fileSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public async Task ServeContentAsync(HttpListenerResponse response, byte[] buffer, string contentType)
        {
            response.ContentLength64 = buffer.Length;
            response.ContentType = contentType;

            using (var outputStream = response.OutputStream)
            {
                await outputStream.WriteAsync(buffer, 0, buffer.Length);
                await outputStream.FlushAsync();
            }
#if DEBUG
            _logger.LogInfo($"Content served: {contentType}, {buffer.Length} bytes");
#endif
        }

        public async Task ServeFileAsync(HttpListenerResponse response, string filePath, string contentType)
        {
#if DEBUG
            _logger.LogInfo($"Reading file: {filePath}");
#endif
            byte[] buffer;
            if (_fileCache.TryGetValue(filePath, out buffer))
            {
#if DEBUG
                _logger.LogInfo($"File served from cache: {filePath}");
#endif
            }
            else
            {
                buffer = await _fileSystem.ReadFileAsync(filePath);
                _fileCache[filePath] = buffer;
#if DEBUG
                _logger.LogInfo($"File read and cached: {filePath}");
#endif
            }

            await ServeContentAsync(response, buffer, contentType);
        }
    }
}


