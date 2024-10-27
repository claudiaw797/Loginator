// Copyright (C) 2024 Claudia Wagner

using System;
using System.Windows.Data;

namespace Loginator.Converter {

    public class KeyToResourceStringConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value is null) return null;

            string? format = parameter?.ToString();

            var key = string.IsNullOrEmpty(format) ? value.ToString() : string.Format(format, value);
            return App.GetStringResource(key!);
        }

        public object? ConvertBack(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
