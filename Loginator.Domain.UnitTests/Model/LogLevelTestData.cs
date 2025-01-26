// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Model;

namespace Loginator.Domain.UnitTests.Model {

    /// <summary>
    /// Represents test data for <see cref="LogLevelTests"/>.
    /// </summary>
    internal static class LogLevelTestData {

        public static readonly object[] ValidLevels = [
            new object[] { LogLevel.TRACE },
            new object[] { LogLevel.DEBUG },
            new object[] { LogLevel.INFO },
            new object[] { LogLevel.WARN },
            new object[] { LogLevel.ERROR },
            new object[] { LogLevel.FATAL },
        ];

        public static readonly object[] FirstLowerThanSecond = [
            new object[] { LogLevel.NOT_SET, LogLevel.TRACE },
            new object[] { LogLevel.TRACE, LogLevel.DEBUG },
            new object[] { LogLevel.TRACE, LogLevel.INFO },
            new object[] { LogLevel.TRACE, LogLevel.WARN },
            new object[] { LogLevel.TRACE, LogLevel.ERROR },
            new object[] { LogLevel.TRACE, LogLevel.FATAL },

            new object[] { LogLevel.TRACE, LogLevel.DEBUG },
            new object[] { LogLevel.TRACE, LogLevel.INFO },
            new object[] { LogLevel.TRACE, LogLevel.WARN },
            new object[] { LogLevel.TRACE, LogLevel.ERROR },
            new object[] { LogLevel.TRACE, LogLevel.FATAL },

            new object[] { LogLevel.DEBUG, LogLevel.INFO },
            new object[] { LogLevel.DEBUG, LogLevel.WARN },
            new object[] { LogLevel.DEBUG, LogLevel.ERROR },
            new object[] { LogLevel.DEBUG, LogLevel.FATAL },

            new object[] { LogLevel.INFO, LogLevel.WARN },
            new object[] { LogLevel.INFO, LogLevel.ERROR },
            new object[] { LogLevel.INFO, LogLevel.FATAL },

            new object[] { LogLevel.WARN, LogLevel.ERROR },
            new object[] { LogLevel.WARN, LogLevel.FATAL },

            new object[] { LogLevel.ERROR,LogLevel.FATAL },
        ];

        public static readonly object[] LevelsBetweenEmptyResult = [
            new object[] { null!, null! },

            new object[] { LogLevel.NOT_SET, null! },
            new object[] { LogLevel.TRACE, null! },
            new object[] { LogLevel.DEBUG, null! },
            new object[] { LogLevel.INFO, null! },
            new object[] { LogLevel.WARN, null! },
            new object[] { LogLevel.ERROR, null! },
            new object[] { LogLevel.FATAL, null! },

            new object[] { null!, LogLevel.NOT_SET },
            new object[] { null!, LogLevel.TRACE },
            new object[] { null!, LogLevel.DEBUG },
            new object[] { null!, LogLevel.INFO },
            new object[] { null!, LogLevel.WARN },
            new object[] { null!, LogLevel.ERROR },
            new object[] { null!, LogLevel.FATAL },

            new object[] { LogLevel.NOT_SET, LogLevel.NOT_SET },
            new object[] { LogLevel.TRACE, LogLevel.TRACE },
            new object[] { LogLevel.DEBUG, LogLevel.DEBUG },
            new object[] { LogLevel.INFO, LogLevel.INFO },
            new object[] { LogLevel.WARN, LogLevel.WARN },
            new object[] { LogLevel.ERROR, LogLevel.ERROR },
            new object[] { LogLevel.FATAL, LogLevel.FATAL },
        ];

        public static readonly object[] LevelsBetween = [
            new object[] { LogLevel.NOT_SET, LogLevel.TRACE, new LogLevel[] { LogLevel.TRACE, LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.DEBUG, new LogLevel[] { LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.INFO, new LogLevel[] { LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.WARN, new LogLevel[] { LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.ERROR, new LogLevel[] { LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.FATAL, new LogLevel[] { LogLevel.FATAL }},

            new object[] { LogLevel.TRACE, LogLevel.DEBUG, new LogLevel[] { LogLevel.TRACE }},
            new object[] { LogLevel.TRACE, LogLevel.INFO, new LogLevel[] { LogLevel.TRACE, LogLevel.DEBUG }},
            new object[] { LogLevel.TRACE, LogLevel.WARN, new LogLevel[] { LogLevel.TRACE, LogLevel.DEBUG, LogLevel.INFO }},
            new object[] { LogLevel.TRACE, LogLevel.ERROR, new LogLevel[] { LogLevel.TRACE, LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN }},
            new object[] { LogLevel.TRACE, LogLevel.FATAL, new LogLevel[] { LogLevel.TRACE, LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR }},

            new object[] { LogLevel.DEBUG, LogLevel.INFO, new LogLevel[] { LogLevel.DEBUG }},
            new object[] { LogLevel.DEBUG, LogLevel.WARN, new LogLevel[] { LogLevel.DEBUG, LogLevel.INFO }},
            new object[] { LogLevel.DEBUG, LogLevel.ERROR, new LogLevel[] { LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN }},
            new object[] { LogLevel.DEBUG, LogLevel.FATAL, new LogLevel[] { LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR }},

            new object[] { LogLevel.INFO, LogLevel.WARN, new LogLevel[] { LogLevel.INFO }},
            new object[] { LogLevel.INFO, LogLevel.ERROR, new LogLevel[] { LogLevel.INFO, LogLevel.WARN }},
            new object[] { LogLevel.INFO, LogLevel.FATAL, new LogLevel[] { LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR }},

            new object[] { LogLevel.WARN, LogLevel.ERROR, new LogLevel[] { LogLevel.WARN }},
            new object[] { LogLevel.WARN, LogLevel.FATAL, new LogLevel[] { LogLevel.WARN, LogLevel.ERROR }},

            new object[] { LogLevel.ERROR,LogLevel.FATAL, new LogLevel[] { LogLevel.ERROR }},
        ];

        public static readonly object[] LevelsBetweenInvalid = [
            new object[] { LogLevel.NOT_SET, LogLevel.TRACE, new LogLevel[] { LogLevel.TRACE, LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.DEBUG, new LogLevel[] { LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.INFO, new LogLevel[] { LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.WARN, new LogLevel[] { LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.ERROR, new LogLevel[] { LogLevel.ERROR, LogLevel.FATAL }},
            new object[] { LogLevel.NOT_SET, LogLevel.FATAL, new LogLevel[] { LogLevel.FATAL }},
        ];
    }
}