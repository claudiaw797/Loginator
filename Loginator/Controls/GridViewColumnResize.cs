﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loginator.Controls {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    
    /// <summary>
    /// http://lazycowprojects.tumblr.com/post/7063214400/wpf-c-listview-column-width-auto
    /// </summary>
    public static class GridViewColumnResize {

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.RegisterAttached(DependencyPropertyConstants.WIDTH, typeof(string), typeof(GridViewColumnResize),
                                                new PropertyMetadata(OnSetWidthCallback));

        public static readonly DependencyProperty GridViewColumnResizeBehaviorProperty =
            DependencyProperty.RegisterAttached(DependencyPropertyConstants.GRID_VIEW_COLUMN_RESIZE_BEHAVIOUR,
                                                typeof(GridViewColumnResizeBehavior), typeof(GridViewColumnResize),
                                                null);

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(DependencyPropertyConstants.ENABLED, typeof(bool), typeof(GridViewColumnResize),
                                                new PropertyMetadata(OnSetEnabledCallback));

        public static readonly DependencyProperty ListViewResizeBehaviorProperty =
            DependencyProperty.RegisterAttached(DependencyPropertyConstants.LIST_VIEW_RESIZE_BEHAVIOUR_PROPERTY,
                                                typeof(ListViewResizeBehavior), typeof(GridViewColumnResize), null);

        public static string GetWidth(DependencyObject obj) {
            return (string)obj.GetValue(WidthProperty);
        }

        public static void SetWidth(DependencyObject obj, string value) {
            obj.SetValue(WidthProperty, value);
        }

        public static bool GetEnabled(DependencyObject obj) {
            return (bool)obj.GetValue(EnabledProperty);
        }

        public static void SetEnabled(DependencyObject obj, bool value) {
            obj.SetValue(EnabledProperty, value);
        }

        private static void OnSetWidthCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) {
            var element = dependencyObject as GridViewColumn;
            if (element != null) {
                GridViewColumnResizeBehavior behavior = GetOrCreateBehavior(element);
                behavior.Width = e.NewValue as string;
            } else {
                Console.Error.WriteLine("Error: Expected type GridViewColumn but found " + dependencyObject.GetType().Name);
            }
        }

        private static void OnSetEnabledCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) {
            var element = dependencyObject as ListView;
            if (element != null) {
                ListViewResizeBehavior behavior = GetOrCreateBehavior(element);
                behavior.Enabled = (bool)e.NewValue;
            } else {
                Console.Error.WriteLine("Error: Expected type ListView but found " + dependencyObject.GetType().Name);
            }
        }

        private static ListViewResizeBehavior GetOrCreateBehavior(ListView element) {
            var behavior = element.GetValue(ListViewResizeBehaviorProperty) as ListViewResizeBehavior;
            if (behavior == null) {
                behavior = new ListViewResizeBehavior(element);
                element.SetValue(ListViewResizeBehaviorProperty, behavior);
            }

            return behavior;
        }

        private static GridViewColumnResizeBehavior GetOrCreateBehavior(GridViewColumn element) {
            var behavior = element.GetValue(GridViewColumnResizeBehaviorProperty) as GridViewColumnResizeBehavior;
            if (behavior == null) {
                behavior = new GridViewColumnResizeBehavior(element);
                element.SetValue(GridViewColumnResizeBehaviorProperty, behavior);
            }

            return behavior;
        }

        /// <summary>
        /// GridViewColumn class that gets attached to the GridViewColumn control
        /// </summary>
        public class GridViewColumnResizeBehavior {
            private readonly GridViewColumn _element;

            public GridViewColumnResizeBehavior(GridViewColumn element) {
                _element = element;
            }

            public string Width { get; set; }

            public bool IsStatic {
                get { return StaticWidth >= 0; }
            }

            public double StaticWidth {
                get {
                    double result;
                    return double.TryParse(Width, out result) ? result : -1;
                }
            }

            public double Percentage {
                get {
                    if (!IsStatic) {
                        return Mulitplier * 100;
                    }
                    return 0;
                }
            }

            public double Mulitplier {
                get {
                    if (Width == "*" || Width == "1*") return 1;
                    if (Width.EndsWith("*")) {
                        double perc;
                        if (double.TryParse(Width.Substring(0, Width.Length - 1), out perc)) {
                            return perc;
                        }
                    }
                    return 1;
                }
            }

            public void SetWidth(double allowedSpace, double totalPercentage) {
                if (IsStatic) {
                    _element.Width = StaticWidth;
                } else {
                    double width = allowedSpace * (Percentage / totalPercentage);
                    if (width > 0.0) {
                        _element.Width = width;
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

            private readonly ListView _element;
            private readonly Timer _timer;
            public ListViewResizeBehavior(ListView element) {
                if (element == null) throw new ArgumentNullException("element");
                _element = element;
                element.Loaded += OnLoaded;

                // Action for resizing and re-enable the size lookup. This stops the columns from constantly resizing to improve performance
                Action resizeAndEnableSize = () => {
                    Resize();
                    _element.SizeChanged += OnSizeChanged;
                };
                _timer = new Timer(x => Application.Current.Dispatcher.BeginInvoke(resizeAndEnableSize), null, Delay,
                                    RefreshTime);
            }

            public bool Enabled { get; set; }


            private void OnLoaded(object sender, RoutedEventArgs e) {
                _element.SizeChanged += OnSizeChanged;
            }

            private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
                if (e.WidthChanged) {
                    _element.SizeChanged -= OnSizeChanged;
                    _timer.Change(Delay, RefreshTime);
                }
            }

            private void Resize() {
                if (Enabled) {
                    double totalWidth = _element.ActualWidth;
                    var gv = _element.View as GridView;
                    if (gv != null && Math.Abs(totalWidth) > 0.0) {
                        double allowedSpace = totalWidth - GetAllocatedSpace(gv);
                        allowedSpace = allowedSpace - Margin;
                        double totalPercentage = GridViewColumnResizeBehaviors(gv).Sum(x => x.Percentage);
                        foreach (GridViewColumnResizeBehavior behavior in GridViewColumnResizeBehaviors(gv)) {
                            behavior.SetWidth(allowedSpace, totalPercentage);
                        }
                    }
                }
            }

            private static IEnumerable<GridViewColumnResizeBehavior> GridViewColumnResizeBehaviors(GridView gv) {
                foreach (GridViewColumn t in gv.Columns) {
                    var gridViewColumnResizeBehavior = t.GetValue(GridViewColumnResizeBehaviorProperty) as GridViewColumnResizeBehavior;
                    if (gridViewColumnResizeBehavior != null) {
                        yield return gridViewColumnResizeBehavior;
                    }
                }
            }

            private static double GetAllocatedSpace(GridView gv) {
                double totalWidth = 0;
                foreach (GridViewColumn t in gv.Columns) {
                    var gridViewColumnResizeBehavior = t.GetValue(GridViewColumnResizeBehaviorProperty) as GridViewColumnResizeBehavior;
                    if (gridViewColumnResizeBehavior != null) {
                        if (gridViewColumnResizeBehavior.IsStatic) {
                            totalWidth += gridViewColumnResizeBehavior.StaticWidth;
                        }
                    } else {
                        totalWidth += t.ActualWidth;
                    }
                }
                return totalWidth;
            }
        }
    }
}
