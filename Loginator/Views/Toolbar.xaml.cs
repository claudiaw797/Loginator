// Copyright (C) 2024 Claudia Wagner

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
    }
}
