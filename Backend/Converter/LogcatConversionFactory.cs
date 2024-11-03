// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Domain.Converter;
using Loginator.Domain.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using static Loginator.Domain.Common.Constants;
using LogLevel = Loginator.Domain.Model.LogLevel;

namespace Loginator.Infrastructure.Converter {

    public partial class LogcatConversionFactory(ILogger<LogcatConversionFactory> logger) : ILogConversionFactory {

        private static readonly string[] separator = ["\r\n", "\n", "\r"];

        public IReadOnlyCollection<Log> Convert(Stream stream) =>
            Convert(new StreamReader(stream).ReadToEnd());

        public IReadOnlyCollection<Log> Convert(string text) {
            if (text is null) {
                return [Log.DEFAULT];
            }

            try {
                string[] lines = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                var logs = new List<Log>();

                // Example: I/ActivityManager(  585): Starting activity: Intent { action=android.intent.action...}
                // Namespace: Create "Logcat.585.ActivityManager" from "ActivityManager(  585)"
                foreach (string line in lines) {

                    if (!LogLineRegex().IsMatch(line)) {
                        continue;
                    }

                    var log = new Log();

                    foreach (Match match in LogLineRegex().Matches(line)) {
                        var group = match.Groups;
                        log.Level = GetLogLevel(group[1].Value);
                        log.Namespace = group[3].Value.Trim();
                        log.Namespace = $"{NamespaceLogcat}{NamespaceSplitter}{group[5].Value.Trim()}{NamespaceSplitter}{log.Namespace}";
                        log.Message = group[8].Value.Trim();
                    }

                    if (!string.IsNullOrEmpty(log.Message)) {
                        logs.Add(log);
                    }
                }
                return [.. logs];
            }
            catch (Exception e) {
                logger.LogError(e, "Could not read logcat data");
            }

            return [Log.DEFAULT];
        }

        // https://regex101.com/
        [GeneratedRegex(@"^(V|D|I|W|E|F|S)(\/)([ -~]+)(\()([0-9 ]+)(\))(\:)([ -~]+)$")]
        private static partial Regex LogLineRegex();

        private static LogLevel GetLogLevel(string logLevel) {
            var shortName = logLevel?.Length == 1 ? System.Convert.ToChar(logLevel) : '-';
            var level = LogLevel.FromShortName(shortName) ?? LogLevel.NOT_SET;
            return level;
        }
    }
}
