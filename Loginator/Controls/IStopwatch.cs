using System;

namespace Loginator.Controls {

    public interface IStopwatch {

        /// <summary>
        /// Gets the elapsed time between the starting timestamp (saved at the last call of <see cref="Start"/>) and the time of this call.
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting timestamp and the time of this call./></returns>
        TimeSpan ElapsedTime { get; }

        /// <summary>
        /// Saves the current time for subsequent calls of <see cref="ElapsedTime"/> or <see cref="TraceElapsedTime"/>.
        /// </summary>
        void Start();

        /// <summary>Writes the elapsed time between the starting timestamp (saved on the last call of <see cref="Start"/> and the time of this call at the <see cref="LogLevel.Trace"/> level.</summary>
        /// <param name="message">The message to prefix the elapsed time with.</param>
        void TraceElapsedTime(string message);
    }
}
