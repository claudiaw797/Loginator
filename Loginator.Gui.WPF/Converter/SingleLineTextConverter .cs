// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Gui.WPF.Converter {

    public class SingleLineTextConverter : IValueConverter {

        private const string STRING_NEWLINE_WIN = "\r\n";
        private const string STRING_NEWLINE_UNIX = "\n";
        private const string STRING_SPACING = " ";

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is null) {
                return null;
            }

            string s = (string)value;
            s = s.Replace(STRING_NEWLINE_WIN, STRING_SPACING);
            s = s.Replace(STRING_NEWLINE_UNIX, STRING_SPACING);
            return s;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
