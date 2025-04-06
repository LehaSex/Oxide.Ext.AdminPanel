
using System;

namespace Oxide.Ext.AdminPanel
{
    public interface IDependencyContainer
    {
        void Register<TService, TImplementation>() where TImplementation : TService;
        void Register<TService>(Func<TService> factory);
        void Register<TService>(string name, Func<TService> factory);
        object Resolve(Type serviceType);
        TService Resolve<TService>();
        TService Resolve<TService>(string name);

    }
}
