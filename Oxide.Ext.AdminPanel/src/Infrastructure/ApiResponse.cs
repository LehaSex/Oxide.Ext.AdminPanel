namespace Oxide.Ext.AdminPanel
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object? Data { get; set; }

        public ApiResponse(bool success, string message, object? data)
        {
            Success = success;
            Message = message;
            Data = data;
        }
    }

}
