// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Service;
using Microsoft.Extensions.Logging;
using System;

namespace Loginator.Infrastructure.Service {

    public class StopwatchEnabled(TimeProvider timeProvider, ILogger<StopwatchEnabled> logger) : IStopwatch {

        private long start;

        private readonly TimeProvider timeProvider = timeProvider;
        private readonly ILogger<StopwatchEnabled> logger = logger;

        /// <inheritdoc/>
        public TimeSpan ElapsedTime =>
            timeProvider.GetElapsedTime(start);

        /// <inheritdoc/>
        public void Start() =>
            start = timeProvider.GetTimestamp();

        /// <inheritdoc/>
        public void TraceElapsedTime(string message) =>
            logger.LogTrace("{message} {time:G}", message, timeProvider.GetElapsedTime(start));
    }
}
