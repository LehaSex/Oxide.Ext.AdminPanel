using System;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public interface IWebServer : IDisposable
    {
        Task StartAsync();
        Task StopAsync();
    }
}