// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Application.Model;
using Loginator.Application.Option;
using Loginator.Application.ViewModel;
using Loginator.Gui.WPF.Control;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Loginator.Gui.WPF.View {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private const string templateAppName = "{0} v{1}";

        private readonly AssemblyInfo assemblyInfo;
        private readonly ILogger<MainWindow> logger;

        public MainWindow(AssemblyInfo assemblyInfo, IOptions<ApplicationOptions> options, ILogger<MainWindow> logger) {
            this.assemblyInfo = assemblyInfo;
            this.logger = logger;

            InitializeComponent();
            this.Title = string.Format(templateAppName, assemblyInfo.Product, assemblyInfo.VersionName);

            if (options.Value.CheckForUpdateOnStartup) {
                Task.Run(() => this.CheckForNewVersion());
            }

            if (DataContext is LoginatorViewModel vm) {
                vm.OnCopyToClipboard = s => Clipboard.SetText(s);

                try {
                    vm.StartMessageProcessing();
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                }
            }
        }

        public ScrollViewerBehavior.RowResize ApplicationsRowBehavior => new(ApplicationsSplitter, ApplicationsRow, 250);

        public ScrollViewerBehavior.RowResize SelectedLogRowBehavior => new(SelectedLogSplitter, SelectedLogRow, 250);

        internal async Task CheckForNewVersion(bool loud = false) {
            var path = $"{assemblyInfo.SourceUrl}/{assemblyInfo.AssemblyInfoPath}";
            try {
                using var webClient = new HttpClient();
                using var stream = await webClient.GetStreamAsync(path);

                var latestAssembly = JsonSerializer.Deserialize<AssemblyInfo>(stream);
                if (latestAssembly is not null && latestAssembly.VersionCode > assemblyInfo.VersionCode) {
                    logger.LogInformation($"New version available. Current {assemblyInfo.VersionName} ({assemblyInfo.VersionCode}), latest {latestAssembly.VersionName} ({latestAssembly.VersionCode})");

                    var messageBoxResult = MessageBox.Show(
                        string.Format(App.GetStringResource("msg.UpdateAvailable")!, latestAssembly.VersionName),
                        App.GetStringResource("grp.UpdateCheck"),
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (messageBoxResult == MessageBoxResult.Yes) {
                        Process.Start(assemblyInfo.DownloadUrl);
                    }
                }
                else {
                    logger.LogInformation($"No new version available. Current {assemblyInfo.VersionName} ({assemblyInfo.VersionCode})");

                    if (loud) MessageBox.Show(
                        App.GetStringResource("msg.NoUpdate"),
                        App.GetStringResource("grp.UpdateCheck"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception e) {
                logger.LogError(e, $"Could not check for new version on path {path}");

                if (loud) MessageBox.Show(
                    string.Format(App.GetStringResource("msg.UpdateError")!, e.Message),
                    App.GetStringResource("grp.UpdateCheck"),
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}
