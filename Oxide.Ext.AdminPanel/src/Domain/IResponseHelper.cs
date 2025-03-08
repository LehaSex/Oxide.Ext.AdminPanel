using System.Net;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public interface IResponseHelper
    {
        Task ServeContentAsync(HttpListenerResponse response, byte[] buffer, string contentType);
        Task ServeFileAsync(HttpListenerResponse response, string filePath, string contentType);
    }
}
