// Copyright (C) 2024 Claudia Wagner

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Loginator.Gui.WPF.Control {

    public class MenuItemGroup : MenuItem {

        private readonly MultiBinding multiBinding;

        public MenuItemGroup() {
            this.multiBinding = CreateIsSelectedValueBinding();
            this.ItemContainerStyle = CreateItemContainerStyle(this.multiBinding);
        }

        public required Binding SelectedValue { get; set; }

        protected override DependencyObject GetContainerForItemOverride() =>
            new SelectableMenuItem();

        protected override void OnInitialized(EventArgs e) {
            var mode = this.SelectedValue.Mode;
            var binding = new Binding {
                Source = this,
                Path = new PropertyPath($"{nameof(DataContext)}.{this.SelectedValue.Path.Path}"),
                Mode = mode == BindingMode.Default ? BindingMode.TwoWay : mode
            };
            this.multiBinding.Bindings.Add(binding);
        }

        private static MultiBinding CreateIsSelectedValueBinding() {
            var multiBinding = new MultiBinding();

            var converterParameter = new Binding(nameof(Header)) {
                Mode = BindingMode.OneWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.Self)
            };
            multiBinding.Bindings.Add(converterParameter);

            multiBinding.Converter = new MultiValueConverterAdapter();

            return multiBinding;
        }

        private static Style CreateItemContainerStyle(MultiBinding multiBinding) {
            var itemContainerStyle = new Style(typeof(MenuItem));
            itemContainerStyle.Setters.Add(
                new Setter {
                    Property = IsCheckableProperty,
                    Value = true
                });

            itemContainerStyle.Setters.Add(
                new Setter {
                    Property = SelectableMenuItem.IsSelectedValueProperty,
                    Value = multiBinding
                });
            return itemContainerStyle;
        }

        private class SelectableMenuItem : MenuItem {

            #region IsSelectedValue (Attached Property)
            public static readonly DependencyProperty IsSelectedValueProperty =
                DependencyProperty.Register(
                    "IsSelectedValue",
                    typeof(SelectableItem),
                    typeof(SelectableMenuItem),
                    new FrameworkPropertyMetadata(
                        new SelectableItem(),
                        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                        OnIsSelectedValueChanged));

            public SelectableItem IsSelectedValue {
                get => (SelectableItem)GetValue(IsSelectedValueProperty);
                set => SetValue(IsSelectedValueProperty, value);
            }
            #endregion

            protected override void OnClick() {
                if (!IsChecked) {
                    base.OnClick();

                    IsSelectedValue = new SelectableItem(Header, true);
                }
            }

            private static void OnIsSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
                if (d is MenuItem menuItem &&
                    e.NewValue is SelectableItem newValue &&
                    menuItem.IsChecked != newValue.IsSelected) {
                    menuItem.IsChecked = newValue.IsSelected;
                }
            }
        }

        private class MultiValueConverterAdapter : IMultiValueConverter {

            public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
                if (values.Length != 2) throw new ArgumentException("Need exactly 2 values: first from source, second from target.", nameof(values));

                var paramValue = values[0];
                var result = Equals(paramValue, values[1]);
                return new SelectableItem(paramValue, result);
            }

            public object?[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
                var selectableItem = value as SelectableItem;
                var result = selectableItem?.Item;
                var isSelected = selectableItem?.IsSelected;
                return isSelected.GetValueOrDefault() ? [result, result] : [result, Binding.DoNothing];
            }
        }

        private record SelectableItem(object? Item = null, bool IsSelected = false) { }
    }
}
