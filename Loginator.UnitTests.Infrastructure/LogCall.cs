// Copyright (C) 2025 Claudia Wagner

using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Loginator.UnitTests.Infrastructure {

    public record LogCall {

        public LogCall(LogLevel logLevel, EventId eventId, string? message) {
            this.LogLevel = logLevel;
            this.EventId = eventId;
            this.Message = message;
        }

        public LogLevel @LogLevel { get; init; }

        public EventId @EventId { get; init; }

        public string? Message { get; init; }
    }
}