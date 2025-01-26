// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Model;
using System.Collections.Generic;
using System.Xml.Linq;
using static Loginator.Infrastructure.UnitTests.Converter.Log4jConversionServiceTestData;

namespace Loginator.Infrastructure.UnitTests.Server {

    /// <summary>
    /// Represents test data for <see cref="LogRepositoryTests"/>.
    /// </summary>
    internal class LogRepositoryTestData {

        public static string ValidLogMessage() {
            var input = Log4jFull(false, false, false, SaveOptions.None);
            return input;
        }

        public static IEnumerable<string> ValidLogMessages() {
            bool[] booleans = [true, false];

            foreach (var hasPrefix in booleans) {
                foreach (var hasNamespace in booleans) {
                    foreach (var isMixed in booleans) {
                        foreach (var option in FormatOptions) {
                            var input = Log4jFull(hasPrefix, hasNamespace, isMixed, option);
                            yield return input;
                        }
                    }
                }
            }
        }

        public static Log ValidLog =>
            LogFromFullLog4jXml;
    }
}