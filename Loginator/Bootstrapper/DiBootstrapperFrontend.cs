using AutoMapper;
using Backend.Bootstrapper;
using Backend.Model;
using Common.Configuration;
using Loginator.Controls;
using Loginator.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;

namespace Loginator.Bootstrapper {

    public static class DiBootstrapperFrontend {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Initialize(IServiceCollection services) {
            logger.Debug("Bootstrapping DI: Frontend");

            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<LoginatorViewModel>();
            services.AddTransient<ConfigurationViewModel>();

            // TODO: Move this to separate bootstrapper
            services.AddTransient<IApplicationConfiguration, ApplicationConfiguration>();

            if (new ApplicationConfiguration().IsTimingTraceEnabled) {
                services.AddTransient<IStopwatch, StopwatchEnabled>();
            }
            else {
                services.AddTransient<IStopwatch, StopwatchDisabled>();
            }

            var config = new MapperConfiguration(cfg => cfg.CreateMap<Log, LogViewModel>());
            services.AddSingleton<IMapper>(new Mapper(config));

            DiBootstrapperBackend.Initialize(services);
        }
    }
}
