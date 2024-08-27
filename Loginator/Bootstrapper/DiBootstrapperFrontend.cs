// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Bootstrapper;
using Backend.Model;
using Common.Bootstrapper;
using Common.Configuration;
using Loginator.Controls;
using Loginator.ViewModels;
using Loginator.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using System;
using System.IO;
using System.Linq;

namespace Loginator.Bootstrapper {

    public static class DiBootstrapperFrontend {

        private const string appSettingsDefault = "Config/appsettings.json";
        private const string appSettingsTemplate = "Config/appsettings.{0}.json";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void ConfigureAppSettings(HostBuilderContext context, IConfigurationBuilder configBuilder) {
            configBuilder.AddJsonFile(appSettingsDefault, optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(context.HostingEnvironment.EnvironmentName), optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(Environment.MachineName), optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(Environment.UserName), optional: true, reloadOnChange: true);
        }

        public static void Initialize(HostBuilderContext context, IServiceCollection services) {
            logger.Debug("Bootstrapping DI: Frontend");

            var config = context.Configuration;
            var active = GetActiveAppSettings(context.HostingEnvironment);
            services.ConfigureWritable<Configuration>(config.GetSection(Configuration.SectionName), active);
            services.ConfigureWritable<ApplicationConfiguration>(config.GetSection(ApplicationConfiguration.SectionName), active);

            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<LoginatorViewModel>();
            services.AddTransient<ConfigurationViewModel>();
            services.AddSingleton<MainWindow>();

            if (config.GetAppSettings().IsTimingTraceEnabled) {
                services.AddTransient<IStopwatch, StopwatchEnabled>();
            }
            else {
                services.AddTransient<IStopwatch, StopwatchDisabled>();
            }

            DiBootstrapperBackend.Initialize(services);
        }

        private static string GetAppSettings(string infix) =>
            string.Format(appSettingsTemplate, infix);

        private static string GetActiveAppSettings(IHostEnvironment environment) {
            string[] overrides = [Environment.UserName, Environment.MachineName, environment.EnvironmentName];
            var active = overrides
                .Select(o => GetAppSettings(o))
                .FirstOrDefault(f => File.Exists(f), appSettingsDefault);
            return active!;
        }
    }
}
