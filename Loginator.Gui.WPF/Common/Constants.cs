// Copyright (C) 2024 Claudia Wagner

using System.Text.RegularExpressions;

namespace Loginator.Gui.WPF.Common {

    internal static partial class Constants {

        [GeneratedRegex("^[^0-9]+$")]
        public static partial Regex NumbersOnlyRegex();
    }
}
