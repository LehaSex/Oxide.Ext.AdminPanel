
using System;

namespace Oxide.Ext.AdminPanel
{
    public interface IDependencyContainer
    {
        void Register<TService, TImplementation>() where TImplementation : TService;
        TService Resolve<TService>();
        object Resolve(Type serviceType); 
        void Register<TService>(Func<TService> factory);
    }
}
