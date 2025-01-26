// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System;
using System.Collections.Generic;
using System.Linq;
using static Loginator.Domain.Common.Constants;

namespace Loginator.Domain.Model {

    public class Log : IEquatable<Log> {

        private static readonly Log def = new();

        public Log() {
            Timestamp = DateTimeOffset.Now;
            Level = LogLevel.NOT_SET;
            Namespace = NamespaceDefault;
            Application = ApplicationDefault;
            Properties = [];
        }

        /// <summary>
        /// The date and time the log happened. Either this comes from the logging source or is set when received.
        /// </summary>
        public DateTimeOffset Timestamp { get; internal set; }

        /// <summary>
        /// The log level in the form "INFO", "ERROR", etc. This should always be available.
        /// </summary>
        public LogLevel Level { get; internal set; }

        /// <summary>
        /// The log message. Can be anything the logging source writes. This should always be available.
        /// </summary>
        public string? Message { get; internal set; }

        /// <summary>
        /// The exception details including the stacktrace. May not be available.
        /// </summary>
        public string? Exception { get; internal set; }

        /// <summary>
        /// Gets or sets the machine name of the log. May not be available.
        /// </summary>
        public string? MachineName { get; internal set; }

        /// <summary>
        /// The namespace of the log. May be set to "global" if no namespace is available.
        /// </summary>
        public string Namespace { get; internal set; }

        /// <summary>
        /// The application of the log. May be set to "global" if no application is available.
        /// </summary>
        public string Application { get; internal set; }

        /// <summary>
        /// The process id of the application the log originated from. May not be available.
        /// </summary>
        public string? Process { get; internal set; }

        /// <summary>
        /// The thread id of the logging application. May not be available.
        /// </summary>
        public string? Thread { get; internal set; }

        /// <summary>
        /// The location info of the caller making the logging request. May not be available.
        /// </summary>
        public LocationInfo? Location { get; internal set; }

        /// <summary>
        /// The nested diagnostic contexts of the log. May not be available.
        public string? Context { get; internal set; }

        /// <summary>
        /// The mapped diagnostic contexts and additional properties of the log. May be empty.
        /// </summary>
        public IReadOnlyCollection<Property> Properties { get; private set; }

        public static Log DEFAULT => def;

        public static bool operator ==(Log left, Log right) =>
            Equals(left, right);

        public static bool operator !=(Log left, Log right) =>
            !Equals(left, right);

        public bool Equals(Log? other) =>
            other is not null &&
            Timestamp.Ticks == other.Timestamp.Ticks &&
            Level == other.Level &&
            Message == other.Message &&
            Application == other.Application &&
            Process == other.Process &&
            Namespace == other.Namespace &&
            Thread == other.Thread;

        public override bool Equals(object? obj) =>
            Equals(obj as Log);

        public override int GetHashCode() =>
            HashCode.Combine(
                Timestamp,
                Level,
                Message,
                Application,
                Process,
                Namespace,
                Thread);

        /// <summary>
        /// Adds <paramref name="properties"/> to <see cref="Properties"/> and sorts the result.
        /// </summary>
        /// <param name="properties"></param>
        internal void AddProperties(IEnumerable<Property> properties) {
            var final = Properties.Count > 0 ? Properties.Concat(properties) : properties;
            Properties = [.. final.OrderBy(p => p.Name)];
        }
    }
}
