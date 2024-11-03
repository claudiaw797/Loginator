// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Loginator.Domain.Model {

    [DebuggerDisplay("{Name} ({Id} {ShortName}")]
    public sealed class LogLevel : IComparable<LogLevel> {

        private LogLevel(int id, string name, char shortName) {
            Id = id;
            Name = name;
            ShortName = shortName;
        }

        public int Id { get; private set; }
        public string Name { get; private set; }
        public char ShortName { get; private set; }

        public static readonly LogLevel NOT_SET = new(-1, "[not set]", '-');
        public static readonly LogLevel TRACE = new(0, "TRACE", 'V');
        public static readonly LogLevel DEBUG = new(1, "DEBUG", 'D');
        public static readonly LogLevel INFO = new(2, "INFO", 'I');
        public static readonly LogLevel WARN = new(3, "WARN", 'W');
        public static readonly LogLevel ERROR = new(4, "ERROR", 'E');
        public static readonly LogLevel FATAL = new(5, "FATAL", 'F');
        private static readonly LogLevel INVALID = new(99, "INVALID", '!');

        private static readonly IEnumerable<LogLevel> Levels = [NOT_SET, TRACE, DEBUG, INFO, WARN, ERROR, FATAL, INVALID];

        public static readonly IEnumerable<LogLevel> AllLogLevels = Levels.Skip(1).SkipLast(1);

        public static LogLevel FromId(int id) =>
            AllLogLevels.FirstOrDefault(m => m.Id == id, NOT_SET);

        public static LogLevel FromName(string? name) =>
            AllLogLevels.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase), NOT_SET);

        public static LogLevel FromShortName(char shortName) =>
            AllLogLevels.FirstOrDefault(m => m.ShortName == shortName, NOT_SET);

        /// <summary>
        /// Gets the log level incl. the toLevel if fromLevel != toLevel
        /// Example (remove): fromLevel = Debug, toLevel = Warn -> return = Debug, Info
        /// Example (add): fromLevel = Warn, toLevel = Debug -> return = Info, Debug
        /// </summary>
        public static IEnumerable<LogLevel> GetLogLevelsBetween(ref LogLevel? fromLevel, ref LogLevel? toLevel) {
            if (fromLevel == toLevel || fromLevel == null || toLevel == null) {
                return [];
            }

            if (fromLevel == NOT_SET) fromLevel = INVALID;
            if (toLevel == NOT_SET) toLevel = INVALID;

            (LogLevel lower, LogLevel upper) = fromLevel < toLevel
                ? (fromLevel, toLevel)
                : (toLevel, fromLevel);

            return Levels.Where(m => m >= lower && m < upper);
        }

        public static bool operator <(LogLevel? a, LogLevel? b) => a?.Id < b?.Id;

        public static bool operator <=(LogLevel? a, LogLevel? b) => a?.Id <= b?.Id;

        public static bool operator >(LogLevel? a, LogLevel? b) => a?.Id > b?.Id;

        public static bool operator >=(LogLevel? a, LogLevel? b) => a?.Id >= b?.Id;

        public static bool operator ==(LogLevel? a, LogLevel? b) =>
            a is null ? b is null : b is not null && a.Equals(b);

        public static bool operator !=(LogLevel? a, LogLevel? b) => !(a == b);

        public bool Equals(LogLevel? other) => other is not null && other.Id == this.Id;

        public override bool Equals(object? obj) => Equals(obj as LogLevel);

        public override int GetHashCode() => this.Id.GetHashCode();

        public override string ToString() => Name;

        public int CompareTo(LogLevel? other) =>
            other is null || this > other ? 1 : this < other ? -1 : 0;
    }
}
