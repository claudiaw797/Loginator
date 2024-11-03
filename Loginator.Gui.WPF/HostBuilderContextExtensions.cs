// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Application.Model;
using Loginator.Application.Option;
using Loginator.Application.Service;
using Loginator.Application.ViewModel;
using Loginator.Domain.Option;
using Loginator.Gui.WPF.Common;
using Loginator.Gui.WPF.View;
using Loginator.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Loginator.Gui.WPF {

    public static class HostBuilderContextExtensions {

        private const string appSettingsDefault = "Config/appsettings.json";
        private const string appSettingsTemplate = "Config/appsettings.{0}.json";
        private const string assemblyInfoFile = "Loginator.Gui.WPF.Resources.AssemblyInfo.json";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal static void ConfigureAppSettings(HostBuilderContext context, IConfigurationBuilder configBuilder) =>
            context.ConfigureSettings(configBuilder);

        internal static void ConfigureAppServices(HostBuilderContext context, IServiceCollection services) =>
            context.ConfigureServices(services);

        internal static void ConfigureSettings(this HostBuilderContext context, IConfigurationBuilder configBuilder) {
            configBuilder.AddJsonFile(appSettingsDefault, optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(context.HostingEnvironment.EnvironmentName), optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(Environment.MachineName), optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(Environment.UserName), optional: true, reloadOnChange: true);
        }

        internal static void ConfigureServices(this HostBuilderContext context, IServiceCollection services) {
            logger.Debug("Bootstrapping DI: Gui.WPF");

            var config = context.Configuration;
            var active = GetActiveAppSettings(context.HostingEnvironment);
            services.AddWritableOptions<LogProcessingOptions>(config.GetLogProcessingSection(), active);
            services.AddWritableOptions<ApplicationOptions>(config.GetApplicationSection(), active);

            var assemblyInfo = LoadAssemblyInfo();
            if (assemblyInfo is not null) {
                services.AddSingleton(assemblyInfo);
            }

            services.AddSingleton<StringResources>();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<LoginatorViewModel>();
            services.AddTransient<ConfigurationViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IDispatcher>(new DispatcherImpl());

            if (config.GetAppSettings().TracePerformance) {
                services.AddTransient<IStopwatch, StopwatchEnabled>();
            }
            else {
                services.AddTransient<IStopwatch, StopwatchDisabled>();
            }

            services.AddInfrastructure();
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

        private static AssemblyInfo? LoadAssemblyInfo() {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly?.GetManifestResourceStream(assemblyInfoFile);

            return stream is null
                ? throw new InvalidOperationException("Assembly info resource is missing.")
                : JsonSerializer.Deserialize<AssemblyInfo>(stream);
        }
    }
}
