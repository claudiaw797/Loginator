// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Model;
using System;
using System.Diagnostics;
using System.Text;
using static Common.Constants;

namespace Loginator.ViewModels {

    [DebuggerDisplay("{Timestamp} {Level.Id} {Application}.{Namespace} '{Message}'")]
    public class LogViewModel(Log log) {

        private readonly Log log = log;

        public DateTimeOffset Timestamp => log.Timestamp;
        public LoggingLevel Level => log.Level;
        public string? Message => log.Message;
        public string? Exception => log.Exception;
        public string? MachineName => log.MachineName;
        public string Namespace => log.Namespace;
        public string Application => log.Application;
        public string? Process => log.Process;
        public string? Thread => log.Thread;
        public string? Context => log.Context;
        public string? ClassName => log.Location.ClassName;
        public string? FileName => log.Location.FileName;
        public string? MethodName => log.Location.MethodName;
        public string? LineNumber => log.Location.LineNumber;

        public string ApplicationProcess {
            get {
                return string.IsNullOrEmpty(log.Process) || RegexLog4jApp().IsMatch(log.Application)
                    ? log.Application
                    : $"{log.Application} ({log.Process})";
            }
        }

        public string? Location {
            get {
                var sb = new StringBuilder();
                AppendLine(sb, "Class", ClassName);
                AppendLine(sb, "Method", MethodName);
                if (!string.IsNullOrEmpty(FileName)) {
                    AppendLine(sb, "File", $"{FileName}, line {LineNumber}");
                }
                return sb.ToString().Trim();
            }
        }

        internal Log Log => log;

        public override string ToString() {
            // TODO: Localize this with .resx
            var sb = new StringBuilder();

            AppendLine(sb, Level.ToString(), Timestamp.ToString());
            AppendLine(sb, "Application", Application);
            AppendLine(sb, "Process", Process);
            AppendLine(sb, "Namespace", Namespace);
            AppendLine(sb, "Context", Context);
            AppendLine(sb, "Thread", Thread);
            AppendLine(sb, "Message", Message);
            AppendLine(sb, "Exception", Exception);
            AppendLine(sb, "Host", MachineName);
            AppendLine(sb, "Class", ClassName);
            AppendLine(sb, "Method", MethodName);
            AppendLine(sb, "File", FileName);
            AppendLine(sb, "Line", LineNumber);

            return sb.ToString();
        }

        private static void AppendLine(StringBuilder sb, string label, string? value) {
            if (!string.IsNullOrEmpty(value)) {
                sb.Append(label);
                sb.Append(": ");
                sb.AppendLine(value);
            }
        }
    }
}
