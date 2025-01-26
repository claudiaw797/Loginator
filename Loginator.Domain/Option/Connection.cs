// Copyright (C) 2024 Claudia Wagner

using System.Text.Json.Serialization;

namespace Loginator.Domain.Option {

    public record Connection {

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConnectionType ConnectionType { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogType LogType { get; init; }

        public string? IpAddress { get; init; }

        public int Port { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConnectionState State { get; init; }

        public override string ToString() => $"{ConnectionType}:{Port}";
    }
}
