// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Application.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace Loginator.Gui.WPF.View {

    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window {

        public ConfigurationWindow() {
            InitializeComponent();

            if (DataContext is ConfigurationViewModel vm) {
                vm.OnClose = Close;
                vm.OnError = (k, ex) =>
                    MessageBox.Show(ex.Message, $"Error {k} configuration changes", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
            }
        }

        private void OnKeyUp_Window(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) Close();
        }
    }
}
