using System;
using System.Net;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class ApiGetPerformance : ApiController
    {
        public ApiGetPerformance(Controller controller)
            : base(controller)
        {
        }

        public async Task GetPerformance(HttpListenerContext context)
        {
            int fps = GetFpsFromServer();
            int ping = GetPingFromServer();
            long memoryUsage = GetMemoryUsageFromServer();
            await SendResponse(context.Response, true, "OK", new { FPS = fps, PING = ping, MEMORY_USAGE = memoryUsage });
        }

        private int GetFpsFromServer()
        {
            // Логика получения количества игроков
            return Performance.current.frameRate;
        }

        private int GetPingFromServer()
        {
            return Performance.current.ping;
        }        
        
        private long GetMemoryUsageFromServer()
        {
            return Performance.current.memoryUsageSystem;
        }

    }

}
