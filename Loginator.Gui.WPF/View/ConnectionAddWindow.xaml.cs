// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.ViewModel;
using System.Windows;
using System.Windows.Input;
using static Loginator.Gui.WPF.Common.Constants;

namespace Loginator.Gui.WPF.View {

    /// <summary>
    /// Interaction logic for ConnectionAddWindow.xaml
    /// </summary>
    public partial class ConnectionAddWindow : Window {

        public ConnectionAddWindow(ConnectionsViewModel connectionsViewModel) {
            InitializeComponent();

            var connectionAddViewModel = new ConnectionAddViewModel(connectionsViewModel) {
                OnClose = Close,
                OnError = (k, ex) =>
                    MessageBox.Show(ex.Message, $"Error {k} adding connection", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK)
            };

            DataContext = connectionAddViewModel;
        }

        private void OnKeyUp_Window(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) Close();
        }

        private void OnPreviewTextInput_Port(object sender, TextCompositionEventArgs e) {
            e.Handled = NumbersOnlyRegex().IsMatch(e.Text);
        }
    }
}
