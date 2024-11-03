// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Gui.WPF.Converter {

    [ValueConversion(typeof(object), typeof(bool))]
    public class ObjectToBoolConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            Equals(value, parameter);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            Equals(value, true) ? parameter : Binding.DoNothing;
    }
}
