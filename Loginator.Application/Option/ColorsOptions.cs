// Copyright (C) 2024 Claudia Wagner

using System.Drawing;
using System.Text.Json.Serialization;

namespace Loginator.Application.Option {

    public sealed record ColorsOptions {

        public const string SectionName = "Colors";

        [JsonConverter(typeof(JsonStringColorConverter))]
        public Color LevelTrace { get; set; } = Color.DarkGray;

        [JsonConverter(typeof(JsonStringColorConverter))]
        public Color LevelDebug { get; set; } = Color.Gray;

        [JsonConverter(typeof(JsonStringColorConverter))]
        public Color LevelInfo { get; set; } = Color.Green;

        [JsonConverter(typeof(JsonStringColorConverter))]
        public Color LevelWarn { get; set; } = Color.DarkOrange;

        [JsonConverter(typeof(JsonStringColorConverter))]
        public Color LevelError { get; set; } = Color.Red;

        [JsonConverter(typeof(JsonStringColorConverter))]
        public Color LevelFatal { get; set; } = Color.DarkViolet;

        [JsonConverter(typeof(JsonStringColorConverter))]
        public Color LogHighlight { get; set; } = Color.LightGoldenrodYellow;
    }
}
