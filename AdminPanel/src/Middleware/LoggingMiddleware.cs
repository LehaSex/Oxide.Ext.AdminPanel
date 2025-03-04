using System;
using System.Threading.Tasks;
using System.Net;

namespace Oxide.Ext.AdminPanel
{
    public class LoggingMiddleware : IMiddleware
    {
        private readonly ILogger _logger;

        public LoggingMiddleware(ILogger logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpListenerContext context, Func<Task> next)
        {
            _logger.LogInfo($"Request: {context.Request.Url.AbsolutePath}");
            await next();
        }
    }
}
