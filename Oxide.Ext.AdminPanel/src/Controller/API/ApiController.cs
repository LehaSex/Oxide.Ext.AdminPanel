using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class ApiController
    {
        private readonly Controller _controller;

        public ApiController(Controller controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        protected async Task SendResponse(HttpListenerResponse response, bool success, string message, object? data)
        {
            var responseData = new ApiResponse(success, message, data);
            string jsonResponse = JsonSerializer.Serialize(responseData);
            await _controller.Get_responseHelper().ServeContentAsync(response, Encoding.UTF8.GetBytes(jsonResponse), "application/json");
        }

        protected async Task SendError(HttpListenerResponse response, string errorMessage, int statusCode = 400)
        {
            response.StatusCode = statusCode;
            await SendResponse(response, false, errorMessage, null);
        }
    }
}
