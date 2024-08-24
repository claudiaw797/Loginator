using Backend.Converter;
using Common;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Backend.Bootstrapper {

    public static class DiBootstrapperBackend {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Initialize(IServiceCollection services) {
            logger.Debug("Bootstrapping DI: Backend");

            services.AddSingleton<IReceiver, Receiver>();

            services.AddKeyedTransient<ILogConverter, ChainsawToLogConverter>(LogType.Chainsaw);
            services.AddKeyedTransient<ILogConverter, LogcatToLogConverter>(LogType.Logcat);
        }
    }
}
