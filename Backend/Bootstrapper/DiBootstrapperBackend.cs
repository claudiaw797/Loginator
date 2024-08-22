using Backend.Converter;
using Backend.Dao;
using Backend.Manager;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Backend.Bootstrapper {

    public static class DiBootstrapperBackend {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Initialize(IServiceCollection services) {
            logger.Debug("Bootstrapping DI: Backend");

            services.AddSingleton<IReceiver, Receiver>();
            services.AddSingleton<IConfigurationDao, ConfigurationDaoSettings>();

            if (new ConfigurationDaoSettings().Read().LogType == Common.LogType.CHAINSAW) {
                services.AddTransient<ILogConverter, ChainsawToLogConverter>();
            }
            else {
                services.AddTransient<ILogConverter, LogcatToLogConverter>();
            }
        }
    }
}
