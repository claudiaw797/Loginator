// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System;
using System.Collections.Generic;
using System.Linq;

namespace Loginator.Application.Common {

    internal static class EnumerableExtensions {

        public static IEnumerable<T> Flatten<T>(
            this IEnumerable<T> enumerable,
            Func<T, IEnumerable<T>> getMany) {
            IEnumerable<T> array = enumerable as T[] ?? enumerable.ToArray();
            return array.SelectMany(c => getMany(c).Flatten(getMany)).Concat(array);
        }
    }
}