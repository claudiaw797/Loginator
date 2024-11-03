// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System.Text.RegularExpressions;

namespace Loginator.Domain.Common {

    public static partial class Constants {

        public const string NamespaceSplitter = ".";

        public const string NamespaceDefault = "Global (namespace)";
        public const string ApplicationDefault = "Global (application)";

        public const string NamespaceLogcat = "Logcat";

        [GeneratedRegex(@"^(?<app>.+)\((?<pid>[^)]+)\)\s*$", RegexOptions.Compiled)]
        public static partial Regex Log4jAppRegex();
    }
}
