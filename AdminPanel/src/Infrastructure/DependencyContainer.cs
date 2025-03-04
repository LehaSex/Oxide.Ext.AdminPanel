using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Ext.AdminPanel
{
    public class DependencyContainer : IDependencyContainer
    {
        private readonly Dictionary<Type, Type> _registrations = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>(); // factory storage

        public void Register<TService, TImplementation>() where TImplementation : TService
        {
            _registrations[typeof(TService)] = typeof(TImplementation);
        }

        public void Register<TService>(Func<TService> factory) // implement method for register factory
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _factories[typeof(TService)] = () =>
            {
                var instance = factory();
                if (instance == null)
                {
                    throw new InvalidOperationException($"Factory for {typeof(TService).Name} returned null.");
                }
                return instance;
            };
        }

        public TService Resolve<TService>()
        {
            var serviceType = typeof(TService);

            // check exists factory of this type
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                return (TService)factory();
            }

            // if not exists standart registration
            return (TService)Resolve(serviceType);
        }

        public object Resolve(Type serviceType)
        {
            if (!_registrations.ContainsKey(serviceType))
            {
                throw new InvalidOperationException($"Service {serviceType.Name} is not registered.");
            }

            var implementationType = _registrations[serviceType];
            return CreateInstance(implementationType);
        }

        private object CreateInstance(Type implementationType)
        {
            var constructor = implementationType.GetConstructors().FirstOrDefault();
            if (constructor == null)
            {
                throw new InvalidOperationException($"No public constructor found for {implementationType.Name}.");
            }

            var parameters = constructor.GetParameters();
            var parameterInstances = parameters.Select(p => Resolve(p.ParameterType)).ToArray();

            return constructor.Invoke(parameterInstances);
        }
    }
}
