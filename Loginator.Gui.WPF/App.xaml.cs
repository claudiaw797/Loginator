// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Application.Option;
using Loginator.Application.Service;
using Loginator.Gui.WPF.Common;
using Loginator.Gui.WPF.View;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Extensions.Hosting;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using static Loginator.Gui.WPF.HostBuilderExtensions;

namespace Loginator.Gui.WPF {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application {

        private static StringResources? stringResources;

        private readonly IHost host;
        private readonly Logger logger;

        public App() {
            logger = ConfigureLogging();

            host = Host
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration()
                .ConfigureServices()
                .UseNLog()
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e) {
            try {
                // Exception handlers
                DispatcherUnhandledException += OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                await host.StartAsync().ConfigureAwait(false);

                stringResources = host.Services.GetRequiredService<StringResources>();
                _ = host.Services.GetRequiredService<ColorResources>();

                IoC.ServiceProvider = host.Services;
                Current.Properties.Add(typeof(ServiceProvider), host.Services);

                // Initialize dispatcher helper so we can access UI thread in view model
                host.Services.GetRequiredService<IDispatcher>().Initialize();
                host.Services.GetRequiredService<MainWindow>().Show();

                logger.Info("[App.OnStartup] Application successfully started");
            }
            catch (Exception exception) {
                logger.Fatal(exception, "[App.OnStartup] Error during starting Application");
                ShowException(exception);
                Current.Shutdown();
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e) {
            logger.Info("[App.OnExit] Application is stopping...");

            using (host) {
                await host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }

            base.OnExit(e);
        }

        internal static KnownCulture? CurrentCulture =>
            stringResources?.CurrentCulture;

        internal static TWindow? GetCurrent<TWindow>() where TWindow : Window =>
            Current.Windows.OfType<TWindow>().FirstOrDefault();

        internal static T GetService<T>() where T : notnull =>
            Current.Properties[typeof(ServiceProvider)] is ServiceProvider sp
                ? sp.GetRequiredService<T>()
                : throw new ArgumentException("No service provider found", typeof(T).Name);

        internal static object GetService(Type type) =>
            Current.Properties[typeof(ServiceProvider)] is ServiceProvider sp
                ? sp.GetRequiredService(type)
                : throw new ArgumentException("No service provider found", nameof(type));

        internal static string? GetStringResource(string key) =>
            string.IsNullOrEmpty(key) ? null : Current.FindResource(key)?.ToString();

        private static Exception GetInnerException(Exception exception) =>
                exception.InnerException is null
                    ? exception
                    : GetInnerException(exception.InnerException);

        private static void ShowException(Exception? exception) =>
            MessageBox.Show(exception?.ToString(),
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Stop,
                MessageBoxResult.OK);

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            var innerException = GetInnerException(e.Exception);
            logger.Error(e.Exception, "[App] An unhandled dispatcher exception occurred");
            ShowException(innerException);
            e.Handled = true;
            Current.Shutdown();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var exception = e.ExceptionObject as Exception;
            if (exception is null) {
                logger.Fatal("[App] Unknow error killed application");
            }
            else {
                logger.Fatal(exception, "[App] An unhandled exception occurred and the application is terminating");
            }
            ShowException(exception);
        }
    }
}
