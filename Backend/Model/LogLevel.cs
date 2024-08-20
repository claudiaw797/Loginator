using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Backend.Model {

    [DebuggerDisplay("{Name} ({Id} {ShortName}")]
    public sealed class LoggingLevel : IComparable<LoggingLevel> {

        public int Id { get; private set; }
        public string Name { get; private set; }
        public char ShortName { get; private set; }

        public static readonly LoggingLevel NOT_SET = new(-1, "[not set]", '-');
        public static readonly LoggingLevel TRACE = new(0, "TRACE", 'V');
        public static readonly LoggingLevel DEBUG = new(1, "DEBUG", 'D');
        public static readonly LoggingLevel INFO = new(2, "INFO", 'I');
        public static readonly LoggingLevel WARN = new(3, "WARN", 'W');
        public static readonly LoggingLevel ERROR = new(4, "ERROR", 'E');
        public static readonly LoggingLevel FATAL = new(5, "FATAL", 'F');
        private static readonly LoggingLevel INVALID = new(99, "INVALID", '!');

        private static readonly IEnumerable<LoggingLevel> Levels = [NOT_SET, TRACE, DEBUG, INFO, WARN, ERROR, FATAL, INVALID];

        private LoggingLevel(int id, string name, char shortName) {
            Id = id;
            Name = name;
            ShortName = shortName;
        }

        public static LoggingLevel? FromId(int id) =>
            GetAllLogLevels().FirstOrDefault(m => m.Id == id);

        public static LoggingLevel? FromName(string name) =>
            GetAllLogLevels().FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public static LoggingLevel? FromShortName(char shortName) =>
            GetAllLogLevels().FirstOrDefault(m => m.ShortName == shortName);

        public static IEnumerable<LoggingLevel> GetAllLogLevels() =>
            Levels.Skip(1).SkipLast(1);

        /// <summary>
        /// Gets the log level incl. the toLevel if fromLevel != toLevel
        /// Example (remove): fromLevel = Debug, toLevel = Warn -> return = Debug, Info
        /// Example (add): fromLevel = Warn, toLevel = Debug -> return = Info, Debug
        /// </summary>
        public static IEnumerable<LoggingLevel> GetLogLevelsBetween(ref LoggingLevel? fromLevel, ref LoggingLevel? toLevel) {
            if (fromLevel == toLevel || fromLevel == null || toLevel == null) {
                return [];
            }

            if (fromLevel == NOT_SET) fromLevel = INVALID;
            if (toLevel == NOT_SET) toLevel = INVALID;

            (LoggingLevel lower, LoggingLevel upper) = fromLevel < toLevel
                ? (fromLevel, toLevel)
                : (toLevel, fromLevel);

            return Levels.Where(m => m >= lower && m < upper);
        }

        public static bool operator <(LoggingLevel? a, LoggingLevel? b) => a?.Id < b?.Id;

        public static bool operator <=(LoggingLevel? a, LoggingLevel? b) => a?.Id <= b?.Id;

        public static bool operator >(LoggingLevel? a, LoggingLevel? b) => a?.Id > b?.Id;

        public static bool operator >=(LoggingLevel? a, LoggingLevel? b) => a?.Id >= b?.Id;

        public static bool operator ==(LoggingLevel? a, LoggingLevel? b) =>
            a is null ? b is null : b is not null && a.Equals(b);

        public static bool operator !=(LoggingLevel? a, LoggingLevel? b) => !(a == b);

        public bool Equals(LoggingLevel? other) => other is not null && other.Id == this.Id;

        public override bool Equals(object? obj) => Equals(obj as LoggingLevel);

        public override int GetHashCode() => this.Id.GetHashCode();

        public override string ToString() => Name;

        public int CompareTo(LoggingLevel? other) =>
            other is null || this > other ? 1 : this < other ? -1 : 0;
    }
}
