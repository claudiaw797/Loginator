// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Converter {

    public class ObjectToBoolConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}
