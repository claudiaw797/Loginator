// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Converter;
using Backend.Server;
using Common;
using Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using System;

namespace Backend.Bootstrapper {

    public static class DiBootstrapperBackend {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Initialize(IServiceCollection services) {
            logger.Debug("Bootstrapping DI: Backend");

            services.AddKeyedTransient<ILogConverter, ChainsawToLogConverter>(LogType.Chainsaw);
            services.AddKeyedTransient<ILogConverter, LogcatToLogConverter>(LogType.Logcat);

            services.AddKeyedTransient<AbstractSocket, UdpSocket>(ConnectionType.Udp);
            services.AddKeyedTransient<AbstractSocket, TcpSocket>(ConnectionType.Tcp);

            services.AddReceivers();
        }

        private static void AddReceivers(this IServiceCollection services) {
            ConnectionType[] connectionTypes = [ConnectionType.Udp, ConnectionType.Tcp];
            LogType[] logTypes = [LogType.Chainsaw, LogType.Logcat];

            foreach (var connectionType in connectionTypes) {
                foreach (var logType in logTypes) {
                    services.AddKeyedSingleton<IReceiver>((connectionType, logType), (sp, key) => sp.GetReceiver(((ConnectionType, LogType)?)key));
                }
            }
        }

        private static Receiver GetReceiver(this IServiceProvider serviceProvider, (ConnectionType connectionType, LogType logType)? key) {
            ArgumentNullException.ThrowIfNull(key);

            var socket = serviceProvider.GetRequiredKeyedService<AbstractSocket>(key.Value.connectionType);
            var converter = serviceProvider.GetRequiredKeyedService<ILogConverter>(key.Value.logType);
            var configuration = serviceProvider.GetRequiredService<IOptionsMonitor<ApplicationConfiguration>>();
            var logger = serviceProvider.GetRequiredService<ILogger<Receiver>>();
            return new Receiver(socket, converter, configuration, logger);
        }
    }
}
