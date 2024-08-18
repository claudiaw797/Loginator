using Backend.Model;
using System.Linq;

namespace Loginator.UnitTests.ViewModels {

    /// <summary>
    /// Represents test data for view model tests.
    /// </summary>
    internal class TestData {

        public static readonly object[] AllLogLevels = [
            new object[] { LoggingLevel.NOT_SET },
            new object[] { LoggingLevel.TRACE },
            new object[] { LoggingLevel.DEBUG },
            new object[] { LoggingLevel.INFO },
            new object[] { LoggingLevel.WARN },
            new object[] { LoggingLevel.ERROR },
            new object[] { LoggingLevel.FATAL },
        ];

        public static readonly object[] ValidLogLevels =
            AllLogLevels.Skip(1).ToArray();
    }
}