// Copyright (C) 2024 Claudia Wagner

using System.Windows;
using System.Windows.Controls;

namespace Loginator.Gui.WPF.Control {

    public class ExtendedTreeView : TreeView {

        public ExtendedTreeView() : base() {
            this.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(OnSelectedItemChanged);
        }

        public static readonly new DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                "SelectedItem",
                typeof(object),
                typeof(ExtendedTreeView),
                new UIPropertyMetadata(null));

        public new object SelectedItem {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (base.SelectedItem != null) {
                SetValue(SelectedItemProperty, base.SelectedItem);
            }
        }
    }
}
