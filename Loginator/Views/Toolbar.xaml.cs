// Copyright (C) 2024 Claudia Wagner

using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Loginator.Views {

    /// <summary>
    /// Interaction logic for Toolbar.xaml
    /// </summary>
    public partial class Toolbar : UserControl {

        public Toolbar() {
            InitializeComponent();
        }

        [GeneratedRegex("^[^0-9]+$")]
        private static partial Regex RxNumbersOnly();

        private void OnPreviewTextInput_NumberOfLogsPerLevel(object sender, TextCompositionEventArgs e) {
            e.Handled = RxNumbersOnly().IsMatch(e.Text);
        }

        private void OnClick_About(object sender, RoutedEventArgs e) {
            var aboutWindow = Application.Current.Windows.OfType<AboutWindow>().FirstOrDefault();
            if (aboutWindow is null) new AboutWindow().Show();
            else aboutWindow.Focus();
        }

        private void OnClick_Settings(object sender, RoutedEventArgs e) {
            new ConfigurationWindow().ShowDialog();
        }

        private async void OnClick_CheckForUpdate(object sender, RoutedEventArgs e) {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow is not null) await mainWindow.CheckForNewVersion();
        }
    }
}
