// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Application.Option;
using Loginator.Application.Service;
using Loginator.Gui.WPF.Common;
using Loginator.Gui.WPF.View;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Config;
using NLog.Extensions.Hosting;
using System;
using System.Linq;
using System.Windows;
using static Loginator.Gui.WPF.HostBuilderContextExtensions;
using WindowsApplication = System.Windows.Application;

namespace Loginator.Gui.WPF {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : WindowsApplication {

        private const string nlogConfig = "Config/nlog.config";
        private const string nlogDevConfig = "Config/nlog.Development.config";

        private static StringResources? stringResources;

        private readonly IHost host;
        private readonly Logger logger;

        public App() {
            logger = SetupLogging();

            host = Host
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration(ConfigureAppSettings)
                .ConfigureServices(ConfigureAppServices)
                .UseNLog()
                .Build();
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

                stringResources = host.Services.GetRequiredService<StringResources>();

                // Initialize dispatcher helper so we can access UI thread in view model
                IoC.ServiceProvider = host.Services;

                host.Services.GetRequiredService<IDispatcher>().Initialize();
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

        internal static KnownCulture? CurrentCulture =>
            stringResources?.CurrentCulture;

        internal static string? GetStringResource(string key) =>
            Current.FindResource(key)?.ToString();

        internal static TWindow? GetCurrent<TWindow>() where TWindow : Window =>
            Current.Windows.OfType<TWindow>().FirstOrDefault();

        private static Exception GetInnerException(Exception exception) =>
            exception.InnerException is null
                ? exception
                : GetInnerException(exception.InnerException);

        private static void HandleException(Exception? exception) =>
            MessageBox.Show(exception?.ToString(),
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Stop,
                MessageBoxResult.OK);

        private static Logger SetupLogging() {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var file = env == Environments.Development ? nlogDevConfig : nlogConfig;
            return LogManager
                .Setup()
                .LoadConfiguration(new XmlLoggingConfiguration(file))
                .GetCurrentClassLogger();
        }
    }
}
