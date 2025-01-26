// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Model;
using System.Collections.Generic;

namespace Loginator.Infrastructure.UnitTests.Channel {

    /// <summary>
    /// Represents test data for <see cref="Channel"/> tests.
    /// </summary>
    internal static class TestData {

        private static Log CreateLog(int postfix) =>
            new() { Message = $"TestMessage{postfix}" };

        public static IEnumerable<Log> TestLogs() => [
            CreateLog(1),
            CreateLog(2),
            CreateLog(3),
            CreateLog(4),
            CreateLog(5),
            CreateLog(6)
        ];
    }
}