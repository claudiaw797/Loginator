using Microsoft.Extensions.DependencyInjection;
using System;

namespace Common.Configuration {

    public class IoC {

        private static IServiceProvider? serviceProvider;

        internal static IServiceProvider ServiceProvider {
            private get {
                if (serviceProvider is null) {
                    throw new InvalidOperationException("No service provider was injected");
                }
                return serviceProvider;
            }
            set { serviceProvider = value; }
        }

        public static T Get<T>() where T : notnull =>
            ServiceProvider.GetRequiredService<T>();

        public static T Get<T>(object serviceKey) where T : notnull =>
            ServiceProvider.GetRequiredKeyedService<T>(serviceKey);
    }
}
