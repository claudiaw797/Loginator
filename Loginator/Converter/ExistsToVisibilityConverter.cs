// Copyright (C) 2024 Claudia Wagner

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Loginator.Converter {

    public class ExistsToVisibilityConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            return value is null || value is string s && string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}