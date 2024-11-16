// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Domain.Option;
using Loginator.Infrastructure.Option;
using System.Text.Json.Serialization;

namespace Loginator.Application.Option {

    public sealed record ApplicationOptions {

        public const string SectionName = "Application";

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConnectionType ConnectionType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogType LogType { get; set; }

        public int Port { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public KnownCulture Language { get; set; }

        public bool CheckForUpdateOnStartup { get; set; }

        public bool TracePerformance { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogTimeFormat LogTimeFormat { get; set; }

        public LogProcessingOptions LogProcessing { get; set; } = new();

        public ColorsOptions Colors { get; set; } = new();
    }
}
