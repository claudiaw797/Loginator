// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Common;
using Loginator.ViewModels;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Loginator.Views {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private const string TEMPLATE_APP_NAME = "Loginator v{0}";
        private const string VERSION_CODE = "versionCode";
        private const string VERSION_NAME = "versionName";
        private const string FILE_VERSION = "Loginator.Resources.Version.txt";
        private const string VERSION_URL = "https://raw.githubusercontent.com/claudiaw797/Loginator/master/Loginator/Version.txt";
        private const string DOWNLOAD_URL = "https://github.com/claudiaw797/Loginator/releases";

        private static readonly string[] NEWLINE_SEPARATORS = [Environment.NewLine, Constants.STRING_NEWLINE];
        private static readonly string[] EQUALS_SEPARATORS = ["="];

        [GeneratedRegex("^[^0-9]+$")]
        private static partial Regex RxNumbersOnly();

        private ILogger Logger { get; set; }

        private int Version { get; set; }

        public MainWindow() {
            Logger = LogManager.GetCurrentClassLogger();

            InitializeComponent();
            SetTitleVersionFromFile();
            Task.Run(async () => await CheckForNewVersion());
            if (DataContext is LoginatorViewModel vm) {
                try {
                    vm.StartListener();
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                }
            }
        }

        private void SetTitleVersionFromFile() {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly?.GetManifestResourceStream(FILE_VERSION);
            if (stream is not null) {
                using var reader = new StreamReader(stream);
                string text = reader.ReadToEnd();
                Title = GetVersionName(text);
                Version = GetVersionCode(text);
            }
        }

        private static int GetVersionCode(string text) =>
            GetVersion(text, VERSION_CODE, out var actual)
                ? Convert.ToInt32(actual)
                : 1;

        private static string GetVersionName(string text) =>
            GetVersion(text, VERSION_NAME, out var actual)
                ? string.Format(TEMPLATE_APP_NAME, actual)
                : string.Empty;

        private static bool GetVersion(string current, string expected, out string? actual) {
            var splitted = current.Split(NEWLINE_SEPARATORS, StringSplitOptions.None);
            foreach (var line in splitted) {
                var splittedLine = line.Split(EQUALS_SEPARATORS, StringSplitOptions.None);
                if (splittedLine.Length > 0 && expected.Equals(splittedLine[0].Trim(), StringComparison.OrdinalIgnoreCase)) {
                    actual = splittedLine[1].Trim();
                    return true;
                }
            }
            actual = null;
            return false;
        }

        private async Task CheckForNewVersion() {
            try {
                using var webClient = new HttpClient();
                string text = await webClient.GetStringAsync(VERSION_URL);
                int latestVersion = GetVersionCode(text);
                if (Version < latestVersion) {
                    Logger.Info("New version available. Current: '{0}'. Latest: '{1}'", Version, latestVersion);
                    MessageBoxResult messageBoxResult = MessageBox.Show(L10n.Language.NewVersionAvailable, L10n.Language.UpdateAvailable, MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes) {
                        Process.Start(DOWNLOAD_URL);
                    }
                }
                else {
                    Logger.Info("No new version available. Current: '{0}'", Version);
                }
            }
            catch (Exception e) {
                Logger.Error(e, "Could not check for new version");
            }
        }

        private void OnPreviewTextInput_NumberOfLogsPerLevel(object sender, TextCompositionEventArgs e) {
            e.Handled = RxNumbersOnly().IsMatch(e.Text);
        }
    }
}
