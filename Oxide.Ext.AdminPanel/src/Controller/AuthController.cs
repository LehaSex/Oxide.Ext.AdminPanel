using System;
using System.Net;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class AuthController : Controller
    {
        public AuthController(IFileSystem fileSystem, string htmlPath, ResponseHelper responseHelper)
            : base(fileSystem, htmlPath, responseHelper) // response helper to base class
        {
        }

        [UseMiddleware(typeof(LoggingMiddleware))]
        public async Task HandleRequest(HttpListenerContext context)
        {
            // logic for requests
            await View(context.Response, "login"); // Show login.html from /html/ folder
        }
    }
}