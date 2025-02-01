// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Converter;
using Loginator.Domain.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using LogLevel = Loginator.Domain.Model.LogLevel;

namespace Loginator.Infrastructure.Converter {

    public class PlainTextConversionService(ILogger<PlainTextConversionService> logger) : ILogConversionService {

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

                foreach (string line in lines) {
                    var log = new Log {
                        Level = LogLevel.INFO,
                        Timestamp = DateTimeOffset.Now,
                        Namespace = "Plain Namespace",
                        Message = line
                    };

                    if (!string.IsNullOrEmpty(log.Message)) {
                        logs.Add(log);
                    }
                }
                return [.. logs];
            }
            catch (Exception e) {
                logger.LogError(e, "Could not read plain text data");
            }

            return [Log.DEFAULT];
        }
    }
}
