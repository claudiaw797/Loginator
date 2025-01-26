// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Loginator.Infrastructure.UnitTests {

    internal class LogComparer : IEqualityComparer<Log> {

        public bool Equals(Log? x, Log? y) {
            if (ReferenceEquals(x, y))
                return true;

            if (y is null || x is null)
                return false;

            var b = x.Timestamp == y.Timestamp &&
                x.Level == y.Level &&
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
            hash.Add(obj.Timestamp);
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
