using System;
using System.Collections.Generic;

namespace VoidMart.Core
{
    /// <summary>
    /// Minimal, allocation-free service registry.  Every long-lived system in Void Mart
    /// (audio, save, economy, ads, pooling...) registers here during boot so that gameplay
    /// code never has to hunt for singletons or call <c>FindObjectOfType</c>.
    /// </summary>
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> s_Services = new Dictionary<Type, object>(24);

        public static event Action<Type> ServiceRegistered;

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            s_Services[typeof(T)] = service;
            ServiceRegistered?.Invoke(typeof(T));
        }

        public static void Unregister<T>() where T : class
        {
            s_Services.Remove(typeof(T));
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out var boxed))
            {
                service = boxed as T;
                return service != null;
            }
            service = null;
            return false;
        }

        /// <summary>Returns the service or <c>null</c> when it has not been installed yet.</summary>
        public static T Get<T>() where T : class
        {
            return s_Services.TryGetValue(typeof(T), out var boxed) ? boxed as T : null;
        }

        public static bool Has<T>() where T : class => s_Services.ContainsKey(typeof(T));

        public static void Clear() => s_Services.Clear();
    }
}
