using NLog;
using System;

namespace Loginator.Controls {

    public class StopwatchEnabled(TimeProvider timeProvider) : IStopwatch {

        private long start;

        private readonly TimeProvider timeProvider = timeProvider;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public TimeSpan ElapsedTime =>
            timeProvider.GetElapsedTime(start);

        /// <inheritdoc/>
        public void Start() =>
            start = timeProvider.GetTimestamp();

        /// <inheritdoc/>
        public void TraceElapsedTime(string message) =>
            logger.Trace("{0} {1:G}", message, timeProvider.GetElapsedTime(start));
    }
}
