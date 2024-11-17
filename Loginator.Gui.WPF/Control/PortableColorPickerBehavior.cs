// Copyright (C) 2024 Claudia Wagner

using ColorPicker;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Loginator.Gui.WPF.Control {

    public static class PortableColorPickerBehavior {

        #region SelectedSlider (Attached Property)
        public static readonly DependencyProperty SelectedSliderProperty =
            DependencyProperty.RegisterAttached(
                "SelectedSlider",
                typeof(string),
                typeof(PortableColorPickerBehavior),
                new PropertyMetadata(null, OnSelectedSliderChanged));

        public static string GetSelectedSlider(DependencyObject obj) =>
            (string)obj.GetValue(SelectedSliderProperty);

        public static void SetSelectedSlider(DependencyObject obj, string value) =>
            obj.SetValue(SelectedSliderProperty, value);

        private static void OnSelectedSliderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is PortableColorPicker colorPicker) {
                var sliderParent = FindChildren<TabControl>(colorPicker).First();
                var sliders = FindChildren<TabItem>(sliderParent);
                foreach (var slider in sliders) {
                    if (slider.Header.Equals(e.NewValue)) {
                        sliderParent.SelectedItem = slider;
                    }
                }

                colorPicker.Focusable = true;
                colorPicker.KeyUp += OnKeyUp_PortableColorPicker;
            }
        }
        #endregion

        private static void OnKeyUp_PortableColorPicker(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                var popup = FindChildren<Popup>(sender).First();
                popup.IsOpen = false;
            }
        }

        private static IEnumerable<T> FindChildren<T>(object parent) where T : DependencyObject {
            if (parent is DependencyObject dobj) {
                foreach (var child in LogicalTreeHelper.GetChildren(dobj)) {
                    if (child is T found) {
                        yield return found;
                    }

                    foreach (var grandChild in FindChildren<T>(child)) {
                        yield return grandChild;
                    }
                }
            }
        }
    }
}
