// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.ViewModel;
using Loginator.Domain.Channel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Loginator.Application {

    internal static class ServiceCollectionExtensions {

        public static IServiceCollection AddApplication(this IServiceCollection services) {
            services.TryAddSingleton(TimeProvider.System);

            services.AddTransient<ILogProcessor>(sp => sp.GetRequiredService<LoginatorViewModel>());

            services.AddSingleton<LoginatorViewModel>();
            services.AddTransient<AboutViewModel>();
            services.AddTransient<ConfigurationViewModel>();
            services.AddTransient<ConnectionsViewModel>();

            return services;
        }
    }
}
