// Copyright (C) 2024 Claudia Wagner

using System;
using System.Windows.Data;

namespace Loginator.Converter {

    public class ObjectToFormattedResourceStringConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value is null) value = string.Empty;

            string? format = App.GetStringResource(parameter.ToString()!);

            if (string.IsNullOrEmpty(format)) return value.ToString();

            return string.Format(format, value);
        }

        public object? ConvertBack(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
