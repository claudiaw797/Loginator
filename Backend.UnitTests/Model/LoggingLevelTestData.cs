using Backend.Model;

namespace Backend.UnitTests.Model {

    /// <summary>
    /// Represents test data for <see cref="LoggingLevelTests"/>.
    /// </summary>
    internal class LoggingLevelTestData {

        public static readonly object[] ValidLevels = [
            new object[] { LoggingLevel.TRACE },
            new object[] { LoggingLevel.DEBUG },
            new object[] { LoggingLevel.INFO },
            new object[] { LoggingLevel.WARN },
            new object[] { LoggingLevel.ERROR },
            new object[] { LoggingLevel.FATAL },
        ];

        public static readonly object[] ValidLevelsByName = [
            new object[] { "trace", LoggingLevel.TRACE  },
            new object[] { "debug", LoggingLevel.DEBUG },
            new object[] { "info", LoggingLevel.INFO },
            new object[] { "warn", LoggingLevel.WARN },
            new object[] { "error", LoggingLevel.ERROR },
            new object[] { "fatal", LoggingLevel.FATAL },
        ];

        public static readonly object[] ValidLevelsByShortName = [
            new object[] { 'V', LoggingLevel.TRACE },
            new object[] { 'D', LoggingLevel.DEBUG },
            new object[] { 'I', LoggingLevel.INFO },
            new object[] { 'W', LoggingLevel.WARN },
            new object[] { 'E', LoggingLevel.ERROR },
            new object[] { 'F', LoggingLevel.FATAL },
        ];

        public static readonly object[] FirstLowerThanSecond = [
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.TRACE },
            new object[] { LoggingLevel.TRACE, LoggingLevel.DEBUG },
            new object[] { LoggingLevel.TRACE, LoggingLevel.INFO },
            new object[] { LoggingLevel.TRACE, LoggingLevel.WARN },
            new object[] { LoggingLevel.TRACE, LoggingLevel.ERROR },
            new object[] { LoggingLevel.TRACE, LoggingLevel.FATAL },

            new object[] { LoggingLevel.TRACE, LoggingLevel.DEBUG },
            new object[] { LoggingLevel.TRACE, LoggingLevel.INFO },
            new object[] { LoggingLevel.TRACE, LoggingLevel.WARN },
            new object[] { LoggingLevel.TRACE, LoggingLevel.ERROR },
            new object[] { LoggingLevel.TRACE, LoggingLevel.FATAL },

            new object[] { LoggingLevel.DEBUG, LoggingLevel.INFO },
            new object[] { LoggingLevel.DEBUG, LoggingLevel.WARN },
            new object[] { LoggingLevel.DEBUG, LoggingLevel.ERROR },
            new object[] { LoggingLevel.DEBUG, LoggingLevel.FATAL },

            new object[] { LoggingLevel.INFO, LoggingLevel.WARN },
            new object[] { LoggingLevel.INFO, LoggingLevel.ERROR },
            new object[] { LoggingLevel.INFO, LoggingLevel.FATAL },

            new object[] { LoggingLevel.WARN, LoggingLevel.ERROR },
            new object[] { LoggingLevel.WARN, LoggingLevel.FATAL },

            new object[] { LoggingLevel.ERROR,LoggingLevel.FATAL },
        ];

        public static readonly object[] LevelsBetweenEmptyResult = [
            new object[] { null!, null! },

            new object[] { LoggingLevel.NOT_SET, null! },
            new object[] { LoggingLevel.TRACE, null! },
            new object[] { LoggingLevel.DEBUG, null! },
            new object[] { LoggingLevel.INFO, null! },
            new object[] { LoggingLevel.WARN, null! },
            new object[] { LoggingLevel.ERROR, null! },
            new object[] { LoggingLevel.FATAL, null! },

            new object[] { null!, LoggingLevel.NOT_SET },
            new object[] { null!, LoggingLevel.TRACE },
            new object[] { null!, LoggingLevel.DEBUG },
            new object[] { null!, LoggingLevel.INFO },
            new object[] { null!, LoggingLevel.WARN },
            new object[] { null!, LoggingLevel.ERROR },
            new object[] { null!, LoggingLevel.FATAL },

            new object[] { LoggingLevel.NOT_SET, LoggingLevel.NOT_SET },
            new object[] { LoggingLevel.TRACE, LoggingLevel.TRACE },
            new object[] { LoggingLevel.DEBUG, LoggingLevel.DEBUG },
            new object[] { LoggingLevel.INFO, LoggingLevel.INFO },
            new object[] { LoggingLevel.WARN, LoggingLevel.WARN },
            new object[] { LoggingLevel.ERROR, LoggingLevel.ERROR },
            new object[] { LoggingLevel.FATAL, LoggingLevel.FATAL },
        ];

        public static readonly object[] LevelsBetween = [
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.TRACE, new LoggingLevel[] { LoggingLevel.TRACE, LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.DEBUG, new LoggingLevel[] { LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.INFO, new LoggingLevel[] { LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.WARN, new LoggingLevel[] { LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.ERROR, new LoggingLevel[] { LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.FATAL, new LoggingLevel[] { LoggingLevel.FATAL }},

            new object[] { LoggingLevel.TRACE, LoggingLevel.DEBUG, new LoggingLevel[] { LoggingLevel.TRACE }},
            new object[] { LoggingLevel.TRACE, LoggingLevel.INFO, new LoggingLevel[] { LoggingLevel.TRACE, LoggingLevel.DEBUG }},
            new object[] { LoggingLevel.TRACE, LoggingLevel.WARN, new LoggingLevel[] { LoggingLevel.TRACE, LoggingLevel.DEBUG, LoggingLevel.INFO }},
            new object[] { LoggingLevel.TRACE, LoggingLevel.ERROR, new LoggingLevel[] { LoggingLevel.TRACE, LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN }},
            new object[] { LoggingLevel.TRACE, LoggingLevel.FATAL, new LoggingLevel[] { LoggingLevel.TRACE, LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR }},

            new object[] { LoggingLevel.DEBUG, LoggingLevel.INFO, new LoggingLevel[] { LoggingLevel.DEBUG }},
            new object[] { LoggingLevel.DEBUG, LoggingLevel.WARN, new LoggingLevel[] { LoggingLevel.DEBUG, LoggingLevel.INFO }},
            new object[] { LoggingLevel.DEBUG, LoggingLevel.ERROR, new LoggingLevel[] { LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN }},
            new object[] { LoggingLevel.DEBUG, LoggingLevel.FATAL, new LoggingLevel[] { LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR }},

            new object[] { LoggingLevel.INFO, LoggingLevel.WARN, new LoggingLevel[] { LoggingLevel.INFO }},
            new object[] { LoggingLevel.INFO, LoggingLevel.ERROR, new LoggingLevel[] { LoggingLevel.INFO, LoggingLevel.WARN }},
            new object[] { LoggingLevel.INFO, LoggingLevel.FATAL, new LoggingLevel[] { LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR }},

            new object[] { LoggingLevel.WARN, LoggingLevel.ERROR, new LoggingLevel[] { LoggingLevel.WARN }},
            new object[] { LoggingLevel.WARN, LoggingLevel.FATAL, new LoggingLevel[] { LoggingLevel.WARN, LoggingLevel.ERROR }},

            new object[] { LoggingLevel.ERROR,LoggingLevel.FATAL, new LoggingLevel[] { LoggingLevel.ERROR }},
        ];

        public static readonly object[] LevelsBetweenInvalid = [
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.TRACE, new LoggingLevel[] { LoggingLevel.TRACE, LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.DEBUG, new LoggingLevel[] { LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.INFO, new LoggingLevel[] { LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.WARN, new LoggingLevel[] { LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.ERROR, new LoggingLevel[] { LoggingLevel.ERROR, LoggingLevel.FATAL }},
            new object[] { LoggingLevel.NOT_SET, LoggingLevel.FATAL, new LoggingLevel[] { LoggingLevel.FATAL }},
        ];
    }
}