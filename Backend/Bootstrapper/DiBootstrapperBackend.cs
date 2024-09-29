// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Converter;
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

            services.AddKeyedSingleton(LogType.Chainsaw, (sp, key) => sp.GetReceiver(key));
            services.AddKeyedSingleton(LogType.Logcat, (sp, key) => sp.GetReceiver(key));
        }

        private static IReceiver GetReceiver(this IServiceProvider serviceProvider, object? serviceKey) {
            var converter = serviceProvider.GetRequiredKeyedService<ILogConverter>(serviceKey);
            var configuration = serviceProvider.GetRequiredService<IOptionsMonitor<ApplicationConfiguration>>();
            var logger = serviceProvider.GetRequiredService<ILogger<Receiver>>();
            return new Receiver(converter, configuration, logger);
        }
    }
}
