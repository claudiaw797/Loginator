using Common.Configuration;
using Common.Exceptions;
using Loginator.Bootstrapper;
using Loginator.Controls;
using Loginator.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Config;
using NLog.Extensions.Hosting;
using System;
using System.Windows;

namespace Loginator {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private const string nlogConfig = "Config/nlog.config";

        private readonly IHost host;
        private readonly Logger logger;

        public App() {
            logger = LogManager
                .Setup()
                .LoadConfiguration(new XmlLoggingConfiguration(nlogConfig))
                .GetCurrentClassLogger();

            host = Host
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration(DiBootstrapperFrontend.ConfigureAppSettings)
                .ConfigureServices(DiBootstrapperFrontend.Initialize)
                .UseNLog()
                .Build();
        }

        private static Exception GetInnerException(Exception exception) {
            return exception.InnerException is null
                ? exception
                : GetInnerException(exception.InnerException);
        }

        private static void HandleException(Exception? exception) {
            var message = exception is LoginatorException ? exception.Message : exception?.ToString();
            MessageBox.Show(message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Stop,
                MessageBoxResult.OK);
        }

        protected override async void OnStartup(StartupEventArgs e) {
            try {
                // Exception handlers
                DispatcherUnhandledException += (m, n) => {
                    var exception = n.Exception;
                    var innerException = GetInnerException(exception);
                    logger.Error(exception, "[OnStartup] An unhandled dispatcher exception occurred.");
                    HandleException(innerException);
                    n.Handled = true;
                    Current.Shutdown();
                };
                AppDomain.CurrentDomain.UnhandledException += (m, n) => {
                    var exception = n.ExceptionObject as Exception;
                    if (exception is null) {
                        logger.Fatal("[OnStartup] Unknow error killed application");
                    }
                    else {
                        logger.Fatal(exception, "[OnStartup] An unhandled exception occurred and the application is terminating");
                    }
                    HandleException(exception);
                };

                await host.StartAsync();

                // Initialize dispatcher helper so we can access UI thread in view model
                DispatcherHelper.Initialize();
                IoC.ServiceProvider = host.Services;

                host.Services.GetRequiredService<MainWindow>().Show();

                logger.Info("[OnStartup] Application successfully started");
            }
            catch (Exception exception) {
                logger.Fatal(exception, "[OnStartup] Error during starting Application");
                HandleException(exception);
                Current.Shutdown();
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e) {
            logger.Debug("[OnExit] Application is stopping");

            using (host) {
                await host.StopAsync(TimeSpan.FromSeconds(5));
            }

            base.OnExit(e);
        }
    }
}
