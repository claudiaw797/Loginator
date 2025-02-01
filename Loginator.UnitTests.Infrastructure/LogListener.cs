// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FakeItEasy.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Loginator.UnitTests.Infrastructure {

    public class LogListener : IInterceptionListener {

        private static readonly string[] PARAMETER_NAMES = ["logLevel", "eventId"];

        private readonly List<LogCall> logCalls = [];

        public IReadOnlyCollection<LogCall> LogCalls => logCalls;

        public ILogger<T> Setup<T>() {
            var logger = A.Fake<ILogger<T>>();
            A.CallTo(() => logger.IsEnabled(A<LogLevel>._)).Returns(true);
            Fake.GetFakeManager(logger).AddInterceptionListener(this);
            return logger;
        }

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

        public bool Contains(LogLevel logLevel, EventId? eventId = null, string? messagePattern = null, int expectedTimes = 1) {
            var actualTimes = logCalls.Count(new LogPredicate(logLevel, eventId, messagePattern).Execute);
            return actualTimes == expectedTimes;
        }

        public IEnumerable<LogCall> LogCallsByMinLevel(LogLevel logLevel) {
            IEnumerable<LogCall> logCallsOfLevel = logCalls.Where(logCall => logCall.LogLevel >= logLevel);
            return logCallsOfLevel;
        }

        public int SumFromMessage(LogLevel logLevel, Regex regex, string numberGroup) {
            var processedItemCount = logCalls.ToArray()
                .Where(l => l is not null && l.LogLevel == logLevel && !string.IsNullOrEmpty(l.Message))
                .Select(l => regex.Match(l.Message is null ? string.Empty : l.Message))
                .Sum(m => m is not null && m.Success ? int.Parse(m.Groups[numberGroup].Value) : 0);
            return processedItemCount;
        }

        public void Reset() {
            logCalls.Clear();
        }

        private record LogPredicate(LogLevel LogLevel, EventId? EventId = null, string? MessagePattern = null) {

            public bool Execute(LogCall? logCall) =>
                logCall?.LogLevel == LogLevel &&
                (!EventId.HasValue || logCall.EventId == EventId) &&
                (string.IsNullOrEmpty(MessagePattern) || (logCall.Message != null && Regex.IsMatch(logCall.Message, MessagePattern)));
        }
    }
}