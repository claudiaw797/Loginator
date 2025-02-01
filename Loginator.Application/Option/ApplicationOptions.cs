// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Domain.Option;
using System.Text.Json.Serialization;

namespace Loginator.Application.Option {

    public sealed record ApplicationOptions {

        public const string SectionName = "Application";

        public const string LogProcessingSectionName = $"{SectionName}:{LogProcessingOptions.SectionName}";
        public const string ConnectionsSectionName = $"{SectionName}:{ConnectionsOptions.SectionName}";

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public KnownCulture Language { get; set; }

        public bool CheckForUpdateOnStartup { get; set; }

        public bool TracePerformance { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogTimeFormat LogTimeFormat { get; set; }

        public LogProcessingOptions LogProcessing { get; set; } = new();

        public ConnectionsOptions Connections { get; set; } = [];

        public ColorsOptions Colors { get; set; } = new();
    }
}
