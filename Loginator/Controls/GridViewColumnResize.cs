// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Loginator.Gui.WPF.Control {

    /// <summary>
    /// http://lazycowprojects.tumblr.com/post/7063214400/wpf-c-listview-column-width-auto
    /// </summary>
    public static class GridViewColumnResize {

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.RegisterAttached(
                "Width",
                typeof(string),
                typeof(GridViewColumnResize),
                new PropertyMetadata(OnSetWidthCallback));

        public static readonly DependencyProperty GridViewColumnResizeBehaviorProperty =
            DependencyProperty.RegisterAttached(
                "GridViewColumnResizeBehavior",
                typeof(GridViewColumnResizeBehavior),
                typeof(GridViewColumnResize),
                null);

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(GridViewColumnResize),
                new PropertyMetadata(OnSetEnabledCallback));

        public static readonly DependencyProperty ListViewResizeBehaviorProperty =
            DependencyProperty.RegisterAttached(
                "ListViewResizeBehaviorProperty",
                typeof(ListViewResizeBehavior),
                typeof(GridViewColumnResize),
                null);

        public static string GetWidth(DependencyObject obj) =>
            (string)obj.GetValue(WidthProperty);

        public static void SetWidth(DependencyObject obj, string value) =>
            obj.SetValue(WidthProperty, value);

        public static bool GetEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(EnabledProperty);

        public static void SetEnabled(DependencyObject obj, bool value) =>
            obj.SetValue(EnabledProperty, value);

        private static void OnSetWidthCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) {
            if (dependencyObject is GridViewColumn gridViewColumn) {
                GridViewColumnResizeBehavior behavior = GetOrCreateBehavior(gridViewColumn);
                behavior.Width = e.NewValue as string;
            }
            else {
                Console.Error.WriteLine($"Error: Expected type GridViewColumn but found {dependencyObject.GetType().Name}");
            }
        }

        private static void OnSetEnabledCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) {
            if (dependencyObject is ListView listView) {
                ListViewResizeBehavior behavior = GetOrCreateBehavior(listView);
                behavior.Enabled = (bool)e.NewValue;
            }
            else {
                Console.Error.WriteLine($"Error: Expected type ListView but found {dependencyObject.GetType().Name}");
            }
        }

        private static ListViewResizeBehavior GetOrCreateBehavior(ListView element) {
            if (element.GetValue(ListViewResizeBehaviorProperty) is not ListViewResizeBehavior behavior) {
                behavior = new ListViewResizeBehavior(element);
                element.SetValue(ListViewResizeBehaviorProperty, behavior);
            }

            return behavior;
        }

        private static GridViewColumnResizeBehavior GetOrCreateBehavior(GridViewColumn element) {
            if (element.GetValue(GridViewColumnResizeBehaviorProperty) is not GridViewColumnResizeBehavior behavior) {
                behavior = new GridViewColumnResizeBehavior(element);
                element.SetValue(GridViewColumnResizeBehaviorProperty, behavior);
            }

            return behavior;
        }

        /// <summary>
        /// GridViewColumn class that gets attached to the GridViewColumn control
        /// </summary>
        public class GridViewColumnResizeBehavior(GridViewColumn element) {

            public string? Width { get; set; }

            public bool IsStatic =>
                StaticWidth >= 0;

            public double StaticWidth =>
                double.TryParse(Width, out double result) ? result : -1;

            public double Percentage =>
                IsStatic ? 0 : Mulitplier * 100;

            public double Mulitplier =>
                Width != "*" &&
                Width != "1*" &&
                Width is not null &&
                Width.EndsWith('*') &&
                double.TryParse(Width.AsSpan(0, Width.Length - 1), out double perc)
                ? perc
                : 1;

            public void SetWidth(double allowedSpace, double totalPercentage) {
                if (IsStatic) {
                    element.Width = StaticWidth;
                }
                else {
                    var width = allowedSpace * (Percentage / totalPercentage);
                    if (width > 0.0) {
                        element.Width = width;
                    }
                }
            }
        }

        /// <summary>
        /// ListViewResizeBehavior class that gets attached to the ListView control
        /// </summary>
        public class ListViewResizeBehavior {
            private const int Margin = 25;
            private const long RefreshTime = Timeout.Infinite;
            private const long Delay = 100;

            private readonly ListView element;
            private readonly Timer timer;

            public ListViewResizeBehavior(ListView element) {
                ArgumentNullException.ThrowIfNull(element);

                this.element = element;
                element.Loaded += OnLoaded;

                // Action for resizing and re-enable the size lookup. This stops the columns from constantly resizing to improve performance
                Action resizeAndEnableSize = () => {
                    Resize();
                    this.element.SizeChanged += this.OnSizeChanged;
                };
                timer = new Timer(x => App.Current.Dispatcher.BeginInvoke(resizeAndEnableSize), null, Delay, RefreshTime);
            }

            public bool Enabled { get; set; }

            private static IEnumerable<GridViewColumnResizeBehavior> GridViewColumnResizeBehaviors(GridView gv) {
                foreach (GridViewColumn t in gv.Columns) {
                    if (t.GetValue(GridViewColumnResizeBehaviorProperty) is GridViewColumnResizeBehavior resizeBehavior) {
                        yield return resizeBehavior;
                    }
                }
            }

            private static double GetAllocatedSpace(GridView gv) {
                var totalWidth = 0D;
                foreach (GridViewColumn t in gv.Columns) {
                    if (t.GetValue(GridViewColumnResizeBehaviorProperty) is not GridViewColumnResizeBehavior resizeBehavior) {
                        totalWidth += t.ActualWidth;
                    }
                    else if (resizeBehavior.IsStatic) {
                        totalWidth += resizeBehavior.StaticWidth;
                    }
                }
                return totalWidth;
            }

            private void OnLoaded(object sender, RoutedEventArgs e) =>
                element.SizeChanged += OnSizeChanged;

            private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
                if (e.WidthChanged) {
                    element.SizeChanged -= OnSizeChanged;
                    timer.Change(Delay, RefreshTime);
                }
            }

            private void Resize() {
                if (Enabled) {
                    var totalWidth = element.ActualWidth;
                    if (element.View is GridView gridView && Math.Abs(totalWidth) > 0.0) {
                        var allowedSpace = totalWidth - GetAllocatedSpace(gridView);
                        allowedSpace -= Margin;
                        var totalPercentage = GridViewColumnResizeBehaviors(gridView).Sum(x => x.Percentage);
                        foreach (GridViewColumnResizeBehavior behavior in GridViewColumnResizeBehaviors(gridView)) {
                            behavior.SetWidth(allowedSpace, totalPercentage);
                        }
                    }
                }
            }
        }
    }
}
