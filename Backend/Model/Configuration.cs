// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Common;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace Backend.Model {

    public sealed class Configuration {

        public const string SectionName = "UserSettings";

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConnectionType ConnectionType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogType LogType { get; set; }

        public int Port { get; set; }

        public bool AllowAnonymousLogs { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogTimeFormat LogTimeFormat { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApplicationFormat ApplicationFormat { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public KnownCulture Language { get; set; }
    }

    public static class ConfigurationExtensions {

        public static Configuration GetUserSettings(this IConfiguration configuration)
            => configuration.GetSection(Configuration.SectionName).Get<Configuration>() ?? new Configuration();
    }
}
