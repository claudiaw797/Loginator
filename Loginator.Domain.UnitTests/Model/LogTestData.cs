// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Loginator.Domain.UnitTests.Model {

    /// <summary>
    /// Represents test data for <see cref="LogTests"/>.
    /// </summary>
    internal static class LogTestData {

        public static DateTimeOffset Now = DateTimeOffset.Parse("2025-01-11 15:53:49 +01:00");

        public static Log CreateLog(
            LogLevel? level = null,
            string message = "TestMessage",
            DateTimeOffset? timestamp = null,
            string application = "TestApplication",
            string process = "TestProcess",
            string nspace = "TestNamespace",
            string thread = "TestThread") =>
            new() {
                Timestamp = timestamp ?? Now,
                Level = level ?? LogLevel.INFO,
                Message = message,
                Application = application,
                Process = process,
                Namespace = nspace,
                Thread = thread,
            };

        public class LogComparer : IEqualityComparer<Log> {

            public bool Equals(Log? x, Log? y) {
                if (ReferenceEquals(x, y))
                    return true;

                if (y is null || x is null)
                    return false;

                var b = x.Level == y.Level &&
                    x.Message?.ReplaceLineEndings() == y.Message?.ReplaceLineEndings() &&
                    x.Exception?.ReplaceLineEndings() == y.Exception?.ReplaceLineEndings() &&
                    x.MachineName == y.MachineName &&
                    x.Namespace == y.Namespace &&
                    x.Application == y.Application &&
                    x.Process == y.Process &&
                    x.Thread == y.Thread &&
                    x.Location == y.Location &&
                    x.Context?.ReplaceLineEndings() == y.Context?.ReplaceLineEndings() &&
                    Enumerable.SequenceEqual(x.Properties.OrderBy(p => p.Name), y.Properties.OrderBy(p => p.Name));
                return b;
            }

            public int GetHashCode([DisallowNull] Log obj) {
                var hash = new HashCode();
                hash.Add(obj.Level);
                hash.Add(obj.Message);
                hash.Add(obj.Exception);
                hash.Add(obj.MachineName);
                hash.Add(obj.Namespace);
                hash.Add(obj.Application);
                hash.Add(obj.Process);
                hash.Add(obj.Thread);
                hash.Add(obj.Location);
                hash.Add(obj.Context);
                foreach (var property in obj.Properties) {
                    hash.Add(property);
                }
                return hash.ToHashCode();
            }
        }
    }
}