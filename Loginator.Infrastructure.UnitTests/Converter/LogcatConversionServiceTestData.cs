// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static Loginator.Domain.Common.Constants;

namespace Loginator.Infrastructure.UnitTests.Converter {

    /// <summary>
    /// Represents test data for <see cref="LogcatConversionServiceTests"/>.
    /// </summary>
    internal class LogcatConversionServiceTestData {

        private const int BASE_LINE = 585;

        private static readonly LogData[] LogDatas = [
            new('V', LogLevel.TRACE, BASE_LINE + 33, "ActivityManager", "Starting activity: Intent { action=android.intent.action...}"),
            new('D', LogLevel.DEBUG, BASE_LINE - 30, "MyActivity", "MyClass.getView() — get item number 1"),
            new('I', LogLevel.INFO, BASE_LINE + 3012, "YourActivity.Too", "YourClass.getView() — get item number 2"),
            new('W', LogLevel.WARN, BASE_LINE + 123, "HisManager.Also", "HisClass.getView() — get item number 3"),
            new('E', LogLevel.ERROR, BASE_LINE - 523, "Something.Else.Only", "SomeClass.getView() — get item number 4"),
            new('F', LogLevel.FATAL, BASE_LINE - 438, "HerActivity23", "HerClass.getView() — get item number 5"),
            new('S', LogLevel.NOT_SET, BASE_LINE + 21893, "Somewhere.Else.Too", "SomeClass.getView() — get item number 6"),
        ];

        static LogcatConversionServiceTestData() {
            var i = 0;
            var len = LogDatas.Length;
            var logs = new Log[len];
            var inputs = new string[len];
            foreach (var data in LogDatas) {
                var log = new Log {
                    Level = data.Level,
                    Namespace = $"{NamespaceLogcat}{NamespaceSplitter}{data.Line}{NamespaceSplitter}{data.NameSpace}",
                    Message = data.Message
                };
                logs[i] = log;

                inputs[i++] = $"{data.ShortLevel}/{data.NameSpace}({data.Line,5}):{data.Message}";
            }
            Logs = logs;
            TextInputs = inputs;
        }

        public static IReadOnlyList<Log> Logs { get; }
        public static IReadOnlyList<string> TextInputs { get; }

        internal static IEnumerable<TestCaseData> SingleLineLogcatDataOptions() {
            for (int i = 0; i < TextInputs.Count; i++) {
                yield return new TestCaseData(TextInputs[i], Logs[i]).SetName("{m}{p}");
            }
        }

        internal static IEnumerable<TestCaseData> MultiLineLogcatDataOptions() {
            string[] newLines = ["\r\n", "\n", "\r"];

            foreach (var newLine in newLines) {
                var testCase = string.Join(newLine, TextInputs);
                yield return new TestCaseData(testCase).SetName("{m}{p}");
            }
        }

        internal class LogcatComparer : IEqualityComparer<Log> {

            public bool Equals(Log? x, Log? y) {
                if (ReferenceEquals(x, y))
                    return true;

                if (y is null || x is null)
                    return false;

                return x.Level == y.Level &&
                    x.Message?.ReplaceLineEndings() == y.Message?.ReplaceLineEndings() &&
                    x.Namespace == y.Namespace;
            }

            public int GetHashCode([DisallowNull] Log obj) =>
                HashCode.Combine(obj.Level, obj.Message, obj.Namespace);
        }

        private record LogData(char ShortLevel, LogLevel Level, int Line, string NameSpace, string Message) { }
    }
}