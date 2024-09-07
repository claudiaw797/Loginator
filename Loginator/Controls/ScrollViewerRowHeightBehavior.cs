// Copyright (C) 2024 Claudia Wagner

using System.Windows;
using System.Windows.Controls;

namespace Loginator.Controls {

    /// <summary>
    /// Based on https://stackoverflow.com/questions/64606959/resizing-grid-with-expander-not-working-after-gridsplitter-moved
    /// </summary>
    public static class ScrollViewerRowHeightBehavior {

        #region TargetRow (Attached Property)
        public static readonly DependencyProperty TargetRowProperty =
            DependencyProperty.RegisterAttached(
                DependencyPropertyConstants.TARGETROW_PROPERTY,
                typeof(RowDefinition),
                typeof(ScrollViewerRowHeightBehavior),
                new PropertyMetadata(null, OnTargetRowChanged));

        public static RowDefinition GetTargetRow(DependencyObject obj) =>
            (RowDefinition)obj.GetValue(TargetRowProperty);

        public static void SetTargetRow(DependencyObject obj, RowDefinition value) =>
            obj.SetValue(TargetRowProperty, value);

        private static void OnTargetRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is not ScrollViewer scroller) return;

            if (e.OldValue is null) {
                scroller.IsVisibleChanged += OnVisibleChanged;
                scroller.ScrollChanged += OnScrollChanged;
            }

            if (e.NewValue is null) {
                scroller.IsVisibleChanged -= OnVisibleChanged;
                scroller.ScrollChanged -= OnScrollChanged;
            }
        }
        #endregion

        #region TargetRowPreviousHeight (Attached Property)
        public static readonly DependencyProperty TargetRowPreviousHeightProperty =
            DependencyProperty.RegisterAttached(
                DependencyPropertyConstants.TARGETROW_PREVIOUS_HEIGHT_PROPERTY,
                typeof(GridLength),
                typeof(ScrollViewerRowHeightBehavior),
                new PropertyMetadata(GridLength.Auto));

        public static GridLength GetTargetRowPreviousHeight(DependencyObject obj) =>
            (GridLength)obj.GetValue(TargetRowPreviousHeightProperty);

        public static void SetTargetRowPreviousHeight(DependencyObject obj, GridLength value) =>
            obj.SetValue(TargetRowPreviousHeightProperty, value);
        #endregion

        #region OnVisibleChanged
        private static void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var visible = (bool)e.NewValue;
            if (visible) {
                OnExpanded(sender);
            }
            else {
                OnCollapsed(sender);
            }
        }

        private static void OnCollapsed(object sender) {
            if (TryGetTargetRow(sender, out var scroller, out var targetRow)) {
                SetTargetRowPreviousHeight(scroller, targetRow.Height);
                targetRow.Height = GridLength.Auto;
            }
        }

        private static void OnExpanded(object sender) {
            if (TryGetTargetRow(sender, out var scroller, out var targetRow)) {
                targetRow.Height = GetTargetRowPreviousHeight(scroller);
            }
        }
        #endregion

        #region OnScrollChanged
        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e) {
            var maxHeight = TryGetTargetRow(sender, out var scroller, out var targetRow)
                ? GetMaxHeight(e, scroller, targetRow)
                : 0;

            if (maxHeight > 0) {
                targetRow.MaxHeight = maxHeight;
            }
        }

        private static double GetMaxHeight(ScrollChangedEventArgs e, ScrollViewer scroller, RowDefinition targetRow) {
            var result = 0D;
            // for new content with new maxHeight => calculate
            if (e.ExtentHeightChange != 0) {
                result = GetMaxHeight(scroller);
            }
            // if viewport maxHeight is increased and vertical scrollbar is still visible => increase to desired maxHeight
            // or recalculate (horizontal scrollbar visibility might have changed)
            else if (e.ViewportHeightChange > 0
                && scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible) {

                if (scroller.DesiredSize.Height > targetRow.MaxHeight) {
                    result = scroller.DesiredSize.Height;
                }
                else {
                    result = GetMaxHeight(scroller);
                }
            }
            return result;
        }

        private static double GetMaxHeight(ScrollViewer scroller) {
            double result;
            if (scroller.DesiredSize.Height - SystemParameters.ScrollHeight >= scroller.ExtentHeight) {
                result = scroller.DesiredSize.Height;
            }
            else {
                var extra = scroller.ComputedHorizontalScrollBarVisibility == Visibility.Visible ? SystemParameters.ScrollHeight : 0;
                result = scroller.ExtentHeight + extra;
            }
            return result + 0.01;
        }
        #endregion

        private static bool TryGetTargetRow(object sender, out ScrollViewer scroller, out RowDefinition targetRow) {
            scroller = (sender as ScrollViewer)!;
            targetRow = scroller is null ? null! : GetTargetRow(scroller);
            return targetRow != null;
        }
    }
}
