// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Option;
using Loginator.Application.Service;
using Loginator.Application.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Loginator.Application {

    internal static class ServiceCollectionExtensions {

        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration) {
            services.AddSingleton(TimeProvider.System);

            services.AddKeyedTransient<IStopwatch, StopwatchEnabled>(true);
            services.AddKeyedTransient<IStopwatch, StopwatchDisabled>(false);

            if (configuration.GetAppSettings().TracePerformance) {
                services.AddTransient<IStopwatch, StopwatchEnabled>();
            }
            else {
                services.AddTransient<IStopwatch, StopwatchDisabled>();
            }

            services.AddSingleton<LoginatorViewModel>();
            services.AddTransient<AboutViewModel>();
            services.AddTransient<ConfigurationViewModel>();

            return services;
        }
    }
}
