// Copyright (C) 2024 Claudia Wagner

namespace Backend.Model {

    public record LocationInfo {

        /// <summary>
        /// Gets or sets the fully qualified class name of the caller making the logging request.
        /// </summary>
        public string? ClassName { get; internal set; }

        /// <summary>
        /// Gets or sets the file name of the caller making the logging request.
        /// </summary>
        public string? FileName { get; internal set; }

        /// <summary>
        /// Gets or sets the method name of the caller making the logging request.
        /// </summary>
        public string? MethodName { get; internal set; }

        /// <summary>
        /// Gets or sets the line number of the caller making the logging request.
        /// </summary>
        public string? LineNumber { get; internal set; }
    }
}
