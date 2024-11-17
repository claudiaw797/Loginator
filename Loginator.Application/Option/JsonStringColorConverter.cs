// Copyright (C) 2024 Claudia Wagner

using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loginator.Application.Option {

    public class JsonStringColorConverter : JsonConverter<Color> {

        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var argb = reader.GetInt32();
            var color = Color.FromArgb(argb);
            return color;
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
            var argb = value.ToArgb();
            writer.WriteNumberValue(argb);
        }
    }
}
