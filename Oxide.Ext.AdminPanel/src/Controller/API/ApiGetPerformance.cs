using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class ApiGetPerformance : ApiController, IWebSocketDataProvider
    {
        [WebSocketExpose]
        private int _fps;
        [WebSocketExpose]
        private int _ping;
        [WebSocketExpose]
        private long _memoryUsage;
        public string DataKey => "performance";

        public ApiGetPerformance(Controller controller)
            : base(controller)
        {
        }


        public async Task GetPerformance(HttpListenerContext context)
        {
            GetAll();
            await SendResponse(context.Response, true, "OK", new { FPS = _fps, PING = _ping, MEMORY_USAGE = _memoryUsage });
        }

        private int GetFpsFromServer()
        {
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

        private void GetAll()
        {
            _fps = GetFpsFromServer();
            _ping = GetPingFromServer();
            _memoryUsage = GetMemoryUsageFromServer();
            
        }

        public Dictionary<string, object> GetWebSocketData()
        {
            GetAll();
            return WebSocketExposeHelper.GetExposedValues(this);
        }

    }

}
