using Oxide.Ext.AdminPanel;
using System.Threading.Tasks;
using System;
using System.Net;

public class ResponseHelper : IResponseHelper
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public ResponseHelper(ILogger logger, IFileSystem fileSystem)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task ServeContentAsync(HttpListenerResponse response, byte[] buffer, string contentType)
    {
        response.ContentLength64 = buffer.Length;
        response.ContentType = contentType;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        await response.OutputStream.FlushAsync();
        response.OutputStream.Close();

        _logger.LogInfo($"Content served: {contentType}, {buffer.Length} bytes");
    }

    public async Task ServeFileAsync(HttpListenerResponse response, string filePath, string contentType)
    {
        _logger.LogInfo($"Reading file: {filePath}");
        byte[] buffer = await _fileSystem.ReadFileAsync(filePath);
        _logger.LogInfo($"File read successfully: {filePath}");
        await ServeContentAsync(response, buffer, contentType);
    }
}
