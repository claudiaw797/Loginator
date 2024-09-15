// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Common;
using System;
using System.Collections.Generic;

namespace Backend.Model {

    public class Log {

        /// <summary>
        /// The date and time the log happened. Either this comes from the logging source or is set when received.
        /// </summary>
        public DateTimeOffset Timestamp { get; internal set; }

        /// <summary>
        /// The log level in the form "INFO", "ERROR", etc. This should always be available.
        /// </summary>
        public LoggingLevel Level { get; internal set; }
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
        /// The process id of the application the log originated from.
        /// </summary>
        public string? Process { get; internal set; }

        /// <summary>
        /// The thread id of the logging application. May not be available.
        /// </summary>
        public string? Thread { get; internal set; }

        /// <summary>
        /// The context of the logging application. May not be available.
        /// </summary>
        public string? Context { get; internal set; }

        /// <summary>
        /// The location info of the caller making the logging request. May not be available.
        /// </summary>
        public LocationInfo Location { get; internal set; }

        /// <summary>
        /// Additional properties. May not be available.
        /// </summary>
        public IEnumerable<Property> Properties { get; internal set; }

        public Log() {
            Timestamp = DateTimeOffset.Now;
            Level = LoggingLevel.NOT_SET;
            Namespace = Constants.NAMESPACE_GLOBAL;
            Application = Constants.APPLICATION_GLOBAL;
            Location = new();
            Properties = [];
        }

        private static readonly Log def = new();

        public static Log DEFAULT => def;

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
    }
}
