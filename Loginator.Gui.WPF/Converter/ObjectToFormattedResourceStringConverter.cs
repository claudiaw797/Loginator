// Copyright (C) 2024 Claudia Wagner

using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Gui.WPF.Converter {

    [ValueConversion(typeof(object), typeof(string))]
    public class ObjectToFormattedResourceStringConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            value ??= string.Empty;

            var format = App.GetStringResource(parameter?.ToString()!);

            if (string.IsNullOrEmpty(format)) return value.ToString();

            return string.Format(format, value);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
