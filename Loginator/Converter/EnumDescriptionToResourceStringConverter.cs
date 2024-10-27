// Copyright (C) 2024 Claudia Wagner

using System;
using System.ComponentModel;
using System.Globalization;

namespace Loginator.Converter {

    /// <summary>
    /// Based on https://brianlagunas.com/a-better-way-to-data-bind-enums-in-wpf/
    /// </summary>
    public class EnumDescriptionToResourceStringConverter(Type type) : EnumConverter(type) {

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
            if (destinationType == typeof(string)) {
                if (value is not null) {
                    var stringValue = value.ToString();
                    var fi = value.GetType().GetField(stringValue!);
                    if (fi is not null) {
                        var attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
                        var attribute = attributes is not null && attributes.Length > 0 ? attributes[0].Description : null;
                        var result = string.IsNullOrEmpty(attribute) ? null : App.GetStringResource(attribute);
                        return string.IsNullOrEmpty(result) ? stringValue : result;
                    }
                }

                return string.Empty;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
