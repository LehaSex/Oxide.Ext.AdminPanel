using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public interface IMiddleware
    {
        Task InvokeAsync(HttpListenerContext context, Func<Task> next);
    }
}
