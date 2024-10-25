// Copyright (C) 2024 Claudia Wagner

using Loginator.Bootstrapper;
using Loginator.ViewModels;
using System.Text.RegularExpressions;
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

        private void OnSelectionChanged_Language(object sender, SelectionChangedEventArgs e) {
            if (e.Source is ComboBox combo &&
                combo.SelectedItem is ComboBoxItem selected &&
                selected!.Tag is KnownCulture culture) {
                App.LoadStringResources(culture);

                if (this.DataContext is LoginatorViewModel vm) {
                    vm.RaisePropertyChanged(nameof(LoginatorViewModel.SelectedInitialLogLevel));
                }
            }
        }
    }
}
