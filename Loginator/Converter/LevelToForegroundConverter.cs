// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Domain.Model;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Loginator.Gui.WPF.Converter {

    public class LevelToForegroundConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            var color = (value as LogLevel) switch {
                var l when l == LogLevel.TRACE => Colors.DarkGray,
                var l when l == LogLevel.DEBUG => Colors.Gray,
                var l when l == LogLevel.INFO => Colors.Green,
                var l when l == LogLevel.WARN => Colors.DarkOrange,
                var l when l == LogLevel.ERROR => Colors.Red,
                var l when l == LogLevel.FATAL => Colors.DarkViolet,
                _ => Colors.Black,
            };
            return new SolidColorBrush(color);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException("[ConvertBack] not implemented");
    }
}
