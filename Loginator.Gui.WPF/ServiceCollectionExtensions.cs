// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Option;
using Loginator.Application.Service;
using Loginator.Domain.Option;
using Loginator.Gui.WPF.Common;
using Loginator.Gui.WPF.View;
using Loginator.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Loginator.Gui.WPF {

    internal static class ServiceCollectionExtensions {

        public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration, string activeSettingsFilename) {
            services.AddOptionsRepository<LogProcessingOptions>(configuration.GetLogProcessingSection(), activeSettingsFilename);
            services.AddOptionsRepository<ApplicationOptions>(configuration.GetApplicationSection(), activeSettingsFilename);

            return services;
        }

        public static IServiceCollection AddGui(this IServiceCollection services) {
            services.AddSingleton<IDispatcher>(new DispatcherImpl());
            services.AddSingleton<StringResources>();
            services.AddSingleton<MainWindow>();

            return services;
        }
    }
}
