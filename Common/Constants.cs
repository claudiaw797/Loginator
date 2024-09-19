// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System.Text.RegularExpressions;

namespace Common {

    public static partial class Constants {

        public const int DEFAULT_MAX_NUMBER_OF_LOGS_PER_LEVEL = 1000;

        public const string STRING_NEWLINE = "\n";

        public const string NAMESPACE_SPLITTER = ".";
        public const string NAMESPACE_LOGCAT = "Logcat";
        public const string NAMESPACE_GLOBAL = "Global (namespace)";
        public const string APPLICATION_GLOBAL = "Global (application)";

        [GeneratedRegex(@"^(?<app>.+)\((?<pid>[^)]+)\)\s*$", RegexOptions.Compiled)]
        public static partial Regex RegexLog4jApp();
    }
}
