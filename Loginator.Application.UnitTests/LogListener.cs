// Copyright (C) 2024 Claudia Wagner

using FakeItEasy;
using FakeItEasy.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Loginator.Application.UnitTests {

    internal class LogListener : IInterceptionListener {

        private static readonly string[] PARAMETER_NAMES = ["logLevel", "eventId"];

        private readonly List<LogCall> logCalls = [];

        public IReadOnlyCollection<LogCall> LogCalls => logCalls;

        public void OnBeforeCallIntercepted(IFakeObjectCall interceptedCall) {
        }

        public void OnAfterCallIntercepted(ICompletedFakeObjectCall interceptedCall) {
            if ("Log".Equals(interceptedCall.Method.Name) &&
                PARAMETER_NAMES.All(p => interceptedCall.Arguments.ArgumentNames.Contains(p)) &&
                "state".Equals(interceptedCall.Arguments.ArgumentNames.ElementAt(2))) {
                var logLevel = interceptedCall.Arguments.Get<LogLevel>("logLevel");
                var eventId = interceptedCall.Arguments.Get<EventId>("eventId");
                var message = interceptedCall.Arguments[2]?.ToString();
                logCalls.Add(new LogCall(logLevel, eventId, message));
            }
        }

        public bool Contains(LogLevel logLevel, EventId? eventId = null, string? messagePattern = null) {
            var contains = logCalls.Any(logCall =>
                logCall.LogLevel == logLevel &&
                (!eventId.HasValue || logCall.EventId == eventId) &&
                (string.IsNullOrEmpty(messagePattern) || (logCall.Message != null && Regex.IsMatch(logCall.Message, messagePattern))));
            return contains;
        }

        public IEnumerable<LogCall> LogCallsByMinLevel(LogLevel logLevel) {
            IEnumerable<LogCall> logCallsOfLevel = logCalls.Where(logCall => logCall.LogLevel >= logLevel);
            return logCallsOfLevel;
        }

        public int SumFromMessage(LogLevel logLevel, Regex regex, string numberGroup) {
            var processedItemCount = logCalls.ToArray()
                .Where(l => l.LogLevel == logLevel && !string.IsNullOrEmpty(l.Message))
                .Select(l => regex.Match(l.Message!))
                .Sum(m => m.Success ? int.Parse(m.Groups[numberGroup].Value) : 0);
            return processedItemCount;
        }

        public void Reset() {
            logCalls.Clear();
        }

        internal record LogCall {

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
}