// Copyright (C) 2024 Claudia Wagner

using System;

namespace Loginator.Application.Service {

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
