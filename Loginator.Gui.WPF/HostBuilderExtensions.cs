// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Application;
using Loginator.Application.Model;
using Loginator.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Config;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Loginator.Gui.WPF {

    internal static class HostBuilderExtensions {

        private const string nlogConfig = "Config/nlog.config";
        private const string nlogDevConfig = "Config/nlog.Development.config";

        private const string appSettingsDefault = "Config/appsettings.json";
        private const string appSettingsTemplate = "Config/appsettings.{0}.json";

        private const string assemblyInfoFile = "Loginator.Gui.WPF.Resources.AssemblyInfo.json";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static Logger ConfigureLogging() {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var file = env == Environments.Development ? nlogDevConfig : nlogConfig;
            return LogManager
                .Setup()
                .LoadConfiguration(new XmlLoggingConfiguration(file))
                .GetCurrentClassLogger();
        }

        public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureAppConfiguration(ConfigureAppSettings);

        public static IHostBuilder ConfigureServices(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices(ConfigureServices);

        private static void ConfigureAppSettings(HostBuilderContext context, IConfigurationBuilder configBuilder) {
            logger.Debug("Bootstrapping DI: adding settings from {0}", appSettingsDefault);

            configBuilder.AddJsonFile(appSettingsDefault, optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(context.HostingEnvironment.EnvironmentName), optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(Environment.MachineName), optional: true, reloadOnChange: true)
                         .AddJsonFile(GetAppSettings(Environment.UserName), optional: true, reloadOnChange: true);
        }

        internal static void ConfigureServices(HostBuilderContext context, IServiceCollection services) {
            logger.Debug("Bootstrapping DI: adding services for Gui.WPF");

            var assemblyInfo = LoadAssemblyInfo();
            if (assemblyInfo is not null) {
                services.AddSingleton(assemblyInfo);
            }

            var activeSettings = GetActiveAppSettings(context.HostingEnvironment);
            services.AddConfiguration(context.Configuration, activeSettings);

            services.AddApplication(context.Configuration);
            services.AddInfrastructure();
            services.AddGui();
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
