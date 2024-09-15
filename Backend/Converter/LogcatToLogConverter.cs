// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Model;
using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Backend.Converter {

    public class LogcatToLogConverter : ILogConverter {

        // https://regex101.com/
        private static readonly Regex Regex = new Regex(@"^(V|D|I|W|E|F|S)(\/)([ -~]+)(\()([0-9 ]+)(\))(\:)([ -~]+)$");
        private ILogger Logger { get; set; }

        public LogcatToLogConverter() {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public IReadOnlyCollection<Log> Convert(string text) {
            if (text == null) {
                return new Log[] { Log.DEFAULT };
            }

            try {
                string[] lines = text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

                IList<Log> logs = new List<Log>();

                // Example: I/ActivityManager(  585): Starting activity: Intent { action=android.intent.action...}
                // Namespace: Create "Logcat.585.ActivityManager" from "ActivityManager(  585)"
                foreach (string line in lines) {

                    if (!Regex.IsMatch(line)) {
                        continue;
                    }

                    Log log = new Log();

                    foreach (Match match in Regex.Matches(line)) {
                        var group = match.Groups;
                        log.Level = GetLogLevel(group[1].Value);
                        log.Namespace = group[3].Value.Trim();
                        log.Namespace = Constants.NAMESPACE_LOGCAT + Constants.NAMESPACE_SPLITTER + group[5].Value.Trim() + Constants.NAMESPACE_SPLITTER + log.Namespace;
                        log.Message = group[8].Value.Trim();
                    }

                    if (!String.IsNullOrEmpty(log.Message)) {
                        logs.Add(log);
                    }
                }
                return [.. logs];
            }
            catch (Exception e) {
                Logger.Error(e, "Could not read logcat data");
            }

            return new Log[] { Log.DEFAULT };
        }

        private LoggingLevel GetLogLevel(string logLevel) {
            var shortName = logLevel?.Length == 1 ? System.Convert.ToChar(logLevel) : '-';
            var level = LoggingLevel.FromShortName(shortName) ?? LoggingLevel.NOT_SET;
            return level;
        }
    }
}
