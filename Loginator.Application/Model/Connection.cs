// Copyright (C) 2024 Claudia Wagner

using Loginator.Infrastructure.Option;
using System.Text.Json.Serialization;

namespace Loginator.Application.Model {

    public record Connection {

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConnectionType ConnectionType { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogType LogType { get; init; }

        public string? IpAddress { get; init; }

        public int Port { get; init; }
    }
}
