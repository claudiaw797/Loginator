// Copyright (C) 2024 Claudia Wagner

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Loginator.Controls {

    public static class ScrollViewerBehavior {

        #region RowResize (Attached Property)
        public static readonly DependencyProperty RowResizeProperty =
            DependencyProperty.RegisterAttached(
                DependencyPropertyConstants.ROW_RESIZE_PROPERTY,
                typeof(RowResize),
                typeof(ScrollViewerBehavior),
                new PropertyMetadata(null, OnBehaviorChanged));

        public static RowResize GetRowResize(DependencyObject obj) =>
            (RowResize)obj.GetValue(RowResizeProperty);

        public static void SetRowResize(DependencyObject obj, RowResize value) =>
            obj.SetValue(RowResizeProperty, value);

        private static void OnBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ScrollViewer scrollViewer) {
                if (e.OldValue is RowResize rowResizeOld) {
                    rowResizeOld.Dispose();
                }

                if (e.NewValue is RowResize rowResizeNew) {
                    rowResizeNew.ScrollViewer = scrollViewer;
                }
            }
        }
        #endregion

        /// <summary>
        /// <see cref="RowResize"/> gets attached to a <see cref="ScrollViewer"/>, located within a <see cref="Grid"/> and
        /// resizable by means of a <see cref="GridSplitter"/>. It then handles further resize behavior.
        /// <list>
        ///   <item>It remembers the last scroll row height, when the <see cref="GridSplitter"/> is dropped. This value is initialized from a constructor parameter.</item>
        ///   <item>It prevents resizing the scroll row with the <see cref="GridSplitter"/> beyond the height needed for the <see cref="ScrollViewer"/>'s current content.</item>
        ///   <item>If the <see cref="ScrollViewer"/>'s content changes, the row height is adapted to the smaller of the latter two values.</item>
        ///   <item>If the <see cref="ScrollViewer"/> is not visible, the row cannot be dragged open with the <see cref="GridSplitter"/>.</item>
        /// </list>
        /// </summary>
        public sealed class RowResize : IDisposable {

            private readonly GridSplitter gridSplitter;
            private readonly RowDefinition targetRow;
            private double targetRowDefaultHeight;
            private ScrollViewer? scrollViewer;

            public RowResize(GridSplitter gridSplitter, RowDefinition scrollRow, double scrollRowDefaultHeight) {
                ArgumentNullException.ThrowIfNull(gridSplitter);
                ArgumentNullException.ThrowIfNull(scrollRow);
                ArgumentNullException.ThrowIfNull(scrollRowDefaultHeight);

                this.gridSplitter = gridSplitter;
                this.targetRow = scrollRow;
                this.targetRowDefaultHeight = scrollRowDefaultHeight;

                this.gridSplitter.DragCompleted += OnSplitterChanged;
            }

            public void Dispose() {
                gridSplitter.DragCompleted -= OnSplitterChanged;

                DisposeScrollViewer();
            }

            internal ScrollViewer? ScrollViewer {
                get => scrollViewer;
                set {
                    if (ReferenceEquals(value, scrollViewer)) return;

                    DisposeScrollViewer();
                    scrollViewer = value;

                    if (scrollViewer is not null) {
                        scrollViewer.IsVisibleChanged += OnVisibleChanged;
                        scrollViewer.ScrollChanged += OnScrollChanged;
                    }
                }
            }

            private static double GetMaxHeight(ScrollViewer scroller) {
                var extra = scroller.ComputedHorizontalScrollBarVisibility == Visibility.Visible ? SystemParameters.ScrollHeight : 0;
                var result = scroller.ExtentHeight + extra;
                return result + 0.01;
            }

            private void OnScrollChanged(object sender, ScrollChangedEventArgs e) {
                if (sender is not ScrollViewer scroller) {
                    return;
                }

                // collapsed => prevent opening splitter
                if (scroller.DesiredSize is Size { Height: 0, Width: 0 } &&
                    targetRow.Height == GridLength.Auto) {
                    targetRow.MaxHeight = 0;
                    return;
                }

                // any change =>
                // set row max height to what content needs
                // possibly adapt row height to min(needed, last selected)
                var maxHeight = GetMaxHeight(scroller);
                targetRow.MaxHeight = maxHeight;

                var currentHeight = targetRow.Height.Value;
                if (currentHeight > maxHeight) {
                    targetRow.Height = new(maxHeight);
                }
                else if (!gridSplitter.IsDragging &&
                    currentHeight < targetRowDefaultHeight) {
                    var newHeight = targetRowDefaultHeight <= maxHeight
                        ? targetRowDefaultHeight
                        : maxHeight;
                    targetRow.Height = new(newHeight);
                }
            }

            private void OnSplitterChanged(object sender, DragCompletedEventArgs e) {
                var currentHeight = targetRow.Height.Value;
                if (currentHeight > 0) {
                    targetRowDefaultHeight = targetRow.Height.Value;
                }
            }

            private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
                bool isVisible = (bool)e.NewValue;
                targetRow.Height = isVisible
                    ? new(targetRowDefaultHeight)
                    : GridLength.Auto;
            }

            private void DisposeScrollViewer() {
                if (scrollViewer is not null) {
                    scrollViewer.IsVisibleChanged -= OnVisibleChanged;
                    scrollViewer.ScrollChanged -= OnScrollChanged;
                }
            }
        }
    }
}
