// Copyright (C) 2024 Claudia Wagner

using Backend.Model;
using Common;
using Common.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Loginator.Converter {

    public class TimestampConverter : IValueConverter {

        private readonly IOptionsMonitor<Configuration> configuration;

        public TimestampConverter()
        {
            // TODO: get it injected
            this.configuration = IoC.Get<IOptionsMonitor<Configuration>>(); 
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            var converted = value is DateTimeOffset d && configuration.CurrentValue.LogTimeFormat == LogTimeFormat.ConvertToLocalTime
                ? d.ToLocalTime()
                : value;

            return converted;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}