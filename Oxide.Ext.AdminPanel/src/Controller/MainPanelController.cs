using System;
using System.Net;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class MainPanelController : Controller
    {
        public MainPanelController(IFileSystem fileSystem, string htmlPath, ResponseHelper responseHelper)
            : base(fileSystem, htmlPath, responseHelper) 
        {
        }

        [UseMiddleware(typeof(LoggingMiddleware))]
        public async Task HandleRequest(HttpListenerContext context)
        {
            await View(context.Response, "index"); 
        }
    }
}