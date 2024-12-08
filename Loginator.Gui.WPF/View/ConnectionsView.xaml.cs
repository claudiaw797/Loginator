// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Loginator.Gui.WPF.View {

    /// <summary>
    /// Interaction logic for ConnectionsView.xaml
    /// </summary>
    public partial class ConnectionsView : UserControl {

        public ConnectionsView() {
            InitializeComponent();
        }

        private void OnClick_ConnectionAdd(object sender, RoutedEventArgs e) {
            if (this.DataContext is not ConnectionsViewModel connectionsViewModel)
                throw new InvalidOperationException("Cannot create new connection without ConnectionsViewModel");

            new ConnectionAddWindow(connectionsViewModel).ShowDialog();
        }
    }
}
