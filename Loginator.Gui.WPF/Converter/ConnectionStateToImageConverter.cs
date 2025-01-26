// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Option;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Gui.WPF.Converter {

    [ValueConversion(typeof(ConnectionState), typeof(Uri))]
    public class ConnectionStateToImageConverter : IValueConverter {

        private const string uriTemplate = "/Resources/Status{0}.png";

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is ConnectionState state) {
                var path = state switch {
                    ConnectionState.Running => "Running",
                    ConnectionState.Stopped => "StoppedOutlineGrey",
                    ConnectionState.Paused => "Paused",
                    _ => throw new ArgumentException($"{value} is not a valid connection state", nameof(value)),
                };
                return new Uri(string.Format(uriTemplate, path), UriKind.Relative);
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
