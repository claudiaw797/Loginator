using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Converter {

    public class StringToIntConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            int.TryParse(value?.ToString(), out var result) ? result : -1;

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value?.ToString();
    }
}
