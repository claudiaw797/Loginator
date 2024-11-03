// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using System.Text.Json.Serialization;

namespace Loginator.Domain.Option {

    public sealed class LogProcessingOptions {

        public const string SectionName = "LogProcessing";

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApplicationFormat ApplicationFormat { get; set; }

        public bool AllowAnonymousMessages { get; set; }

        public bool TraceMessages { get; set; }
    }
}
