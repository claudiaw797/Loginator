// Copyright (C) 2024 Claudia Wagner

using System;
using System.Globalization;
using System.Windows.Data;
using MediaColor = System.Windows.Media.Color;
using SystemColor = System.Drawing.Color;

namespace Loginator.Gui.WPF.Converter {

    [ValueConversion(typeof(SystemColor), typeof(MediaColor))]
    public class SystemToMediaColorConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            ArgumentNullException.ThrowIfNull(value);

            var color = (SystemColor)value;
            return MediaColor.FromArgb(color.A, color.R, color.G, color.B);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            ArgumentNullException.ThrowIfNull(value);

            var color = (MediaColor)value;
            return SystemColor.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}