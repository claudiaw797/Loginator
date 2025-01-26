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

    public partial class LogcatConversionService(ILogger<LogcatConversionService> logger) : ILogConversionService {

        private static readonly string[] separator = ["\r\n", "\n", "\r"];

        public IReadOnlyCollection<Log> Convert(Stream stream) =>
            Convert(new StreamReader(stream).ReadToEnd());

        public IReadOnlyCollection<Log> Convert(string text) {
            try {
                var lines = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                var logs = new List<Log>();

                // Example: I/ActivityManager(  585): Starting activity: Intent { action=android.intent.action...}
                // Namespace: Create "Logcat.585.ActivityManager" from "ActivityManager(  585)"
                foreach (string line in lines) {

                    var match = LogLineRegex().Match(line);
                    if (!match.Success) {
                        continue;
                    }

                    var log = new Log {
                        Level = GetLogLevel(match.Groups["level"].Value),
                        Namespace = $"{NamespaceLogcat}{NamespaceSplitter}{match.Groups["pid"].Value.Trim()}{NamespaceSplitter}{match.Groups["nsp"].Value.Trim()}",
                        Message = match.Groups["msg"].Value.Trim()
                    };

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
        [GeneratedRegex(@"^\s*(?<level>[DEFISVW])\s*\/\s*(?<nsp>[^(]+)\((?<pid>[0-9\s]+)\)\s*:(?<msg>.*)$")]
        private static partial Regex LogLineRegex();

        private static LogLevel GetLogLevel(string logLevel) {
            logLevel = logLevel.Trim();
            var shortName = logLevel?.Length == 1 ? System.Convert.ToChar(logLevel) : '-';
            var level = LogLevel.FromShortName(shortName) ?? LogLevel.NOT_SET;
            return level;
        }
    }
}
