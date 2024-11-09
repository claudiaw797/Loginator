// Copyright (C) 2024 Claudia Wagner

using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Gui.WPF.Converter {

    [ValueConversion(typeof(object), typeof(bool))]
    public class IsHitTestVisibleConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            var hasCount = (int)values[0];
            var isSelected = (bool)values[1];
            return hasCount <= 0 || isSelected;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}