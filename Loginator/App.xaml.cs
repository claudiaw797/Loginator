// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

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
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace Loginator {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private const string nlogConfig = "Config/nlog.config";
        private const string nlogDevConfig = "Config/nlog.Development.config";

        private static readonly Dictionary<KnownCulture, ResourceDictionary> stringResources = [];

        private readonly IHost host;
        private readonly Logger logger;

        public App() {
            logger = SetupLogging();

            host = Host
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration(DiBootstrapperFrontend.ConfigureAppSettings)
                .ConfigureServices(DiBootstrapperFrontend.Initialize)
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

                InitializeStringResources();

                await host.StartAsync();

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

        public static KnownCulture CurrentCulture { get; private set; }

        internal static string? GetStringResource(string key) =>
            Current.FindResource(key)?.ToString();

        internal static void LoadStringResources(KnownCulture nextCulture) {
            if (CurrentCulture == nextCulture)
                return;

            if (!stringResources.TryGetValue(nextCulture, out var nextStringResources)) {
                nextStringResources = GetStringResources(nextCulture);
                stringResources.Add(nextCulture, nextStringResources);
            }

            if (CurrentCulture != KnownCulture.English) {
                var currentStringResources = stringResources[CurrentCulture];
                Current.Resources.MergedDictionaries.Remove(currentStringResources);
            }

            if (nextCulture != KnownCulture.English) {
                Current.Resources.MergedDictionaries.Add(nextStringResources);
            }

            CurrentCulture = nextCulture;
        }

        private static KnownCulture GetKnownCulture(string culture) =>
            culture switch {
                var c when c.StartsWith("de") => KnownCulture.German,
                _ => KnownCulture.English,
            };

        private static ResourceDictionary GetStringResources(KnownCulture culture) {
            var infix = culture switch {
                KnownCulture.German => ".de",
                _ => string.Empty,
            };
            var dictionary = new ResourceDictionary {
                Source = new Uri($@"..\Resources\StringResources{infix}.xaml", UriKind.Relative)
            };
            return dictionary;
        }

        private static void InitializeStringResources() {
            CurrentCulture = KnownCulture.English;
            var defaultStringResources = GetStringResources(CurrentCulture);
            stringResources.Add(CurrentCulture, defaultStringResources);
            Current.Resources.MergedDictionaries.Add(defaultStringResources);

            var culture = Thread.CurrentThread.CurrentCulture.ToString();
            LoadStringResources(GetKnownCulture(culture));
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
