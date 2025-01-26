// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System;
using System.Text.RegularExpressions;

namespace Loginator.Application.Common {

    public static partial class Constants {

        public delegate void OnCloseHandler();

        public delegate void OnErrorHandler(string actionKey, Exception exception);

        [GeneratedRegex(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$")]
        public static partial Regex IpAddressRegex();

        internal const int DefaultMaxNumberOfLogsPerLevel = 1000;

        internal static object SyncObject = new();
    }
}
