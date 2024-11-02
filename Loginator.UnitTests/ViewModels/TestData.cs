// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Model;
using System.Linq;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents test data for view model tests.
    /// </summary>
    internal class TestData {

        public static readonly object[] AllLogLevels = [
            new object[] { LogLevel.NOT_SET },
            new object[] { LogLevel.TRACE },
            new object[] { LogLevel.DEBUG },
            new object[] { LogLevel.INFO },
            new object[] { LogLevel.WARN },
            new object[] { LogLevel.ERROR },
            new object[] { LogLevel.FATAL },
        ];

        public static readonly object[] ValidLogLevels =
            AllLogLevels.Skip(1).ToArray();
    }
}