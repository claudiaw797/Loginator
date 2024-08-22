using Microsoft.Extensions.DependencyInjection;
using System;

namespace Common.Configuration {

    public class IoC {

        private static readonly IServiceCollection container = new ServiceCollection();
        private static IServiceProvider? serviceProvider;

        public static IServiceProvider Default =>
            serviceProvider ??= container.BuildServiceProvider();

        public static void Configure(Action<IServiceCollection> configure) {
            configure.Invoke(container);
        }

        public static T Get<T>() where T : notnull =>
            Default.GetRequiredService<T>();
    }
}
