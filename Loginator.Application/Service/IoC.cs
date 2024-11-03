// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Loginator.Application.Service {

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
