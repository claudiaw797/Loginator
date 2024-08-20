using System;

namespace Loginator.Controls {

    public class StopwatchDisabled : IStopwatch {

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <returns><see cref="TimeSpan.Zero"/>.</returns>
        public TimeSpan ElapsedTime => TimeSpan.Zero;

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Start() { }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void TraceElapsedTime(string message) { }
    }
}
