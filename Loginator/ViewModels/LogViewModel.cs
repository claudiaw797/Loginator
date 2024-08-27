// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Model;
using System;
using System.Diagnostics;
using System.Text;

namespace Loginator.ViewModels {

    [DebuggerDisplay("{Timestamp} {Level.Id} {Application}.{Namespace} '{Message}'")]
    public class LogViewModel(Log log) {

        private readonly Log log = log;

        public DateTimeOffset Timestamp => log.Timestamp;
        public LoggingLevel Level => log.Level;
        public string Message => log.Message;
        public string? Exception => log.Exception;
        public string Namespace => log.Namespace;
        public string Application => log.Application;
        public string Thread => log.Thread;
        public string Context => log.Context;
        public string MachineName => log.MachineName;

        internal Log Log => log;

        public override string ToString() {
            // TODO: Localize this with .resx
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{Level}: {Timestamp}");
            sb.Append("Application: ");
            sb.AppendLine(Application);
            sb.Append("Namespace: ");
            sb.AppendLine(Namespace);
            if (!String.IsNullOrEmpty(Context)) {
                sb.Append("Context: ");
                sb.AppendLine(Context);
            }
            if (!String.IsNullOrEmpty(Thread)) {
                sb.Append("Thread: ");
                sb.AppendLine(Thread);
            }
            sb.Append("Message: ");
            sb.AppendLine(Message);
            if (!String.IsNullOrEmpty(Exception)) {
                sb.Append("Exception: ");
                sb.AppendLine(Exception);
            }
            if (MachineName != null) {
                sb.Append("Host: ");
                sb.AppendLine(MachineName);
            }
            return sb.ToString();
        }
    }
}
