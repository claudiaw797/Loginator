// Copyright (C) 2024 Claudia Wagner

using System.Windows;
using System.Windows.Input;

namespace Loginator.Views {

    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window {

        public AboutWindow() {
            InitializeComponent();
        }

        private void OnKeyUp_Window(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) this.Close();
        }
    }
}
