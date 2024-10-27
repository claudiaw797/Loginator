// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Model;
using Loginator.Controls;
using Loginator.Model;
using Loginator.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Loginator.Views {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private const string templateAppName = "{0} v{1}";

        private readonly AssemblyInfo assemblyInfo;
        private readonly ILogger<MainWindow> logger;

        public MainWindow(AssemblyInfo assemblyInfo, IOptions<Configuration> configuration, ILogger<MainWindow> logger) {
            this.assemblyInfo = assemblyInfo;
            this.logger = logger;

            InitializeComponent();
            this.Title = string.Format(templateAppName, assemblyInfo.Product, assemblyInfo.VersionName);

            if (configuration.Value.CheckForUpdateOnStartup) {
                Task.Run(() => this.CheckForNewVersion());
            }

            if (DataContext is LoginatorViewModel vm) {
                try {
                    vm.StartListener();
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                }
            }
        }

        public ScrollViewerBehavior.RowResize GridRowBehavior => new(SplitterRow, SelectedLogRow, 250);

        internal async Task CheckForNewVersion() {
            try {
                var path = $"{assemblyInfo.RawUrl}/AssemblyInfo.json";
                using var webClient = new HttpClient();
                using var stream = await webClient.GetStreamAsync(path);

                var latestAssembly = JsonSerializer.Deserialize<AssemblyInfo>(stream);
                if (latestAssembly is not null && latestAssembly.VersionCode > assemblyInfo.VersionCode) {
                    logger.LogInformation($"New version available. Current: '{assemblyInfo.VersionCode}'. Latest: '{latestAssembly.VersionCode}'");

                    MessageBoxResult messageBoxResult = MessageBox.Show(App.GetStringResource("msg.NewVersionAvailable"), App.GetStringResource("msg.UpdateAvailable"), MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes) {
                        Process.Start(assemblyInfo.DownloadUrl);
                    }
                }
                else {
                    logger.LogInformation($"No new version available. Current: '{assemblyInfo.VersionCode}'");
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Could not check for new version");
            }
        }
    }
}
