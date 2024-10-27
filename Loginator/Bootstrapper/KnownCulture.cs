// Copyright (C) 2024 Claudia Wagner

using Loginator.Converter;
using System.ComponentModel;

namespace Loginator.Bootstrapper {

    [TypeConverter(typeof(EnumDescriptionToResourceStringConverter))]
    public enum KnownCulture {

        [Description("cmd.English")]
        English,

        [Description("cmd.German")]
        German
    }
}
