// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Channel;
using Loginator.Domain.Converter;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Loginator.Domain.Service;
using Loginator.Infrastructure.Channel;
using Loginator.Infrastructure.Converter;
using Loginator.Infrastructure.Option;
using Loginator.Infrastructure.Server;
using Loginator.Infrastructure.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Loginator.Infrastructure {

    public static class ServiceCollectionExtensions {

        internal static readonly IReadOnlyCollection<ConnectionType> ConnectionTypes = [ConnectionType.Udp, ConnectionType.Tcp];
        internal static readonly IReadOnlyCollection<LogType> LogTypes = [LogType.Log4j, LogType.Logcat];

        public static IServiceCollection AddInfrastructure(this IServiceCollection services) {
            services.TryAddSingleton(TimeProvider.System);

            services.AddAbstractSockets();
            services.AddLogConversionServices();
            services.AddLogRepositories();
            services.AddTransient<ILogService, LogService>();

            services.AddStopwatches();

            return services;
        }

        public static void AddOptionsRepository<TOptions>(this IServiceCollection services, IConfigurationSection section, string file = "appsettings.json")
            where TOptions : class, new() {

            services.Configure<TOptions>(section);

            services.AddTransient<IOptionsRepository<TOptions>>(provider => {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var environment = provider.GetRequiredService<IHostEnvironment>();
                var options = provider.GetRequiredService<IOptionsMonitor<TOptions>>();

                return new OptionsRepository<TOptions>(environment, options, (IConfigurationRoot)configuration, section, file);
            });
        }

        private static void AddAbstractSockets(this IServiceCollection services) {
            services.AddKeyedTransient<AbstractSocket, UdpSocket>(ConnectionType.Udp);
            services.AddKeyedTransient<AbstractSocket, TcpSocket>(ConnectionType.Tcp);
        }

        private static void AddLogConversionServices(this IServiceCollection services) {
            services.AddKeyedTransient<ILogConversionService, Log4jConversionService>(LogType.Log4j);
            services.AddKeyedTransient<ILogConversionService, LogcatConversionService>(LogType.Logcat);
        }

        private static void AddLogRepositories(this IServiceCollection services) {
            services.AddTransient<ILogRepositoryFactory>(sp => new LogRepositoryFactory(sp));

            foreach (var connectionType in ConnectionTypes) {
                foreach (var logType in LogTypes) {
                    services.AddKeyedTransient<ILogRepository>(
                        (connectionType, logType),
                        (sp, key) => sp.GetLogRepository(((ConnectionType, LogType)?)key));
                }
            }
        }

        private static LogRepository GetLogRepository(
            this IServiceProvider serviceProvider,
            (ConnectionType connectionType, LogType logType)? key) {
            if (!key.HasValue) throw new ArgumentNullException(nameof(key));

            var socket = serviceProvider.GetRequiredKeyedService<AbstractSocket>(key.Value.connectionType);
            var conversionFactory = serviceProvider.GetRequiredKeyedService<ILogConversionService>(key.Value.logType);
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<LogProcessingOptions>>();
            var logger = serviceProvider.GetRequiredService<ILogger<LogRepository>>();
            return new LogRepository(socket, conversionFactory, optionsMonitor, logger);
        }

        private static void AddStopwatches(this IServiceCollection services) {
            services.AddTransient<IStopwatchFactory>(sp => new StopwatchFactory(sp));

            services.AddKeyedTransient<IStopwatch, StopwatchEnabled>(true);
            services.AddKeyedTransient<IStopwatch, StopwatchDisabled>(false);
        }
    }
}
