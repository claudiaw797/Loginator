// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Option;
using Loginator.Application.ViewModel;
using Loginator.Domain.Channel;
using Loginator.Domain.Service;
using Loginator.Infrastructure.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Loginator.Application {

    internal static class ServiceCollectionExtensions {

        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration) {
            services.TryAddSingleton(TimeProvider.System);

            if (configuration.GetAppSettings().TracePerformance) {
                services.TryAddTransient<IStopwatch, StopwatchEnabled>();
            }
            else {
                services.TryAddTransient<IStopwatch, StopwatchDisabled>();
            }

            services.AddTransient<ILogProcessor>(sp => sp.GetRequiredService<LoginatorViewModel>());

            services.AddSingleton<LoginatorViewModel>();
            services.AddTransient<AboutViewModel>();
            services.AddTransient<ConfigurationViewModel>();
            services.AddTransient<ConnectionsViewModel>();

            return services;
        }
    }
}
