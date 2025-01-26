// Copyright (C) 2025 Claudia Wagner

using System;
using System.Collections.Generic;
using System.Linq;

namespace Loginator.UnitTests.Infrastructure {

    public static class EnumerableExtensions {

        private static readonly Random Random = new();

        public static T[] Shuffle<T>(this IEnumerable<T>? enumerable, int maxValue = 100) =>
            [.. enumerable?.OrderBy(x => Random.Next(1, maxValue))];
    }
}