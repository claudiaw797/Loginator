// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.ViewModels;
using System.Windows;

namespace Loginator.Views {

    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window {

        public ConfigurationWindow() {
            InitializeComponent();

            if (DataContext is ConfigurationViewModel vm) {
                vm.CloseAction = Close;
            }
        }
    }
}
