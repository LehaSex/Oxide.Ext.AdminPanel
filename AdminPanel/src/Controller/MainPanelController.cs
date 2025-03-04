using System;
using System.Net;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class MainPanelController : Controller
    {
        public MainPanelController(IFileSystem fileSystem, string htmlPath, ResponseHelper responseHelper)
            : base(fileSystem, htmlPath, responseHelper) // Передаем responseHelper в базовый класс
        {
        }

        [UseMiddleware(typeof(LoggingMiddleware))]
        public async Task HandleRequest(HttpListenerContext context)
        {
            // Логика для защищенных запросов
            await View(context.Response, "index"); // Отображаем login.html из папки /html/
        }
    }
}