// Copyright (C) 2024 Claudia Wagner

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Loginator.Gui.WPF.Common.Constants;

namespace Loginator.Gui.WPF.View {

    /// <summary>
    /// Interaction logic for Toolbar.xaml
    /// </summary>
    public partial class Toolbar : UserControl {

        public Toolbar() {
            InitializeComponent();
        }

        private void OnPreviewTextInput_NumberOfLogsPerLevel(object sender, TextCompositionEventArgs e) {
            e.Handled = NumbersOnlyRegex().IsMatch(e.Text);
        }

        private void OnClick_About(object sender, RoutedEventArgs e) {
            var aboutWindow = App.GetCurrent<AboutWindow>();
            if (aboutWindow is null) new AboutWindow().Show();
            else aboutWindow.Focus();
        }

        private void OnClick_Settings(object sender, RoutedEventArgs e) {
            new ConfigurationWindow().ShowDialog();
        }

        private async void OnClick_CheckForUpdate(object sender, RoutedEventArgs e) {
            var mainWindow = App.GetCurrent<MainWindow>();
            if (mainWindow is not null) await mainWindow.CheckForNewVersion(loud: true);
        }
    }
}
