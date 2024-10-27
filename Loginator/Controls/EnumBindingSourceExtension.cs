// Copyright (C) 2024 Claudia Wagner

using System;
using System.Windows.Markup;

namespace Loginator.Controls {

    /// <summary>
    /// Source https://brianlagunas.com/a-better-way-to-data-bind-enums-in-wpf/
    /// </summary>
    public class EnumBindingSourceExtension : MarkupExtension {

        private Type? enumType;

        public EnumBindingSourceExtension() { }

        public EnumBindingSourceExtension(Type enumType) {
            EnumType = enumType;
        }

        public Type? EnumType {
            get => enumType;
            set {
                if (value != enumType) {
                    if (value is not null) {
                        var enumType = GetActualType(value);
                        if (!enumType.IsEnum)
                            throw new ArgumentException("Type must an enumeration type.", nameof(value));
                    }

                    enumType = value;
                }
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            if (enumType is null)
                throw new InvalidOperationException($"The property {nameof(EnumType)} must be specified.");

            var actualEnumType = GetActualType(enumType);
            var enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == enumType)
                return enumValues;

            Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }

        private static Type GetActualType(Type type) =>
            Nullable.GetUnderlyingType(type) ?? type;
    }
}
