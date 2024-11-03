// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Option;
using Loginator.Application.Service;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Gui.WPF.Converter {

    public class TimestampConverter : IValueConverter {

        private readonly IOptionsMonitor<ApplicationOptions> optionsMonitor;

        public TimestampConverter() {
            // TODO: get it injected
            this.optionsMonitor = IoC.Get<IOptionsMonitor<ApplicationOptions>>();
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            var converted = value is DateTimeOffset d && optionsMonitor.CurrentValue.LogTimeFormat == LogTimeFormat.ConvertToLocalTime
                ? d.ToLocalTime()
                : value;

            return converted;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}