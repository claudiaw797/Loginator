// Copyright (C) 2024 Claudia Wagner

using Backend.Model;
using Loginator.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Converter {

    public class ViewModelToLogConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            var converted = value is LogViewModel logVm ? logVm.Log : null;
            return converted;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            var converted = value is Log log ? new LogViewModel(log) : null;
            return converted;
        }
    }
}