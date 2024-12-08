// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Option;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Loginator.Infrastructure.Option {

    public class OptionsRepository<TOptions> : IOptionsRepository<TOptions>
        where TOptions : class, new() {

        private static readonly JsonSerializerOptions serializerOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly IOptionsMonitor<TOptions> optionsMonitor;
        private readonly IConfigurationRoot configuration;
        private readonly IConfigurationSection section;
        private readonly string physicalPath;

        public OptionsRepository(
            IHostEnvironment environment,
            IOptionsMonitor<TOptions> optionsMonitor,
            IConfigurationRoot configuration,
            IConfigurationSection section,
            string file) {

            var physicalPath = environment.ContentRootFileProvider.GetFileInfo(file).PhysicalPath
                ?? throw new ArgumentException($"No file found for {file}", nameof(file));

            this.optionsMonitor = optionsMonitor;
            this.configuration = configuration;
            this.section = section;
            this.physicalPath = physicalPath;
        }

        public TOptions Get() => optionsMonitor.CurrentValue;

        public TOptions Get(string name) => optionsMonitor.Get(name);

        public void Save(Action<TOptions> applyChanges) {
            // create json object from current file
            var jsonFile = File.Exists(physicalPath) ?
                JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(physicalPath))
                : [];

            var (jsonParent, jsonChild) = Nodes(this.section.Path, jsonFile);
            var sectionObject = jsonChild is null
                ? new TOptions()
                : JsonSerializer.Deserialize<TOptions>(jsonChild.ToString()) ?? new();

            // apply changes to section
            applyChanges(sectionObject);

            jsonParent[this.section.Key] = JsonObject.Parse(JsonSerializer.Serialize(sectionObject));

            // serialize json and write to file
            File.WriteAllText(physicalPath, JsonSerializer.Serialize(jsonFile, serializerOptions));

            configuration.Reload();
        }

        public IDisposable? OnChanged(Action<TOptions, string?> listener) =>
            optionsMonitor.OnChange(listener);

        private static (JsonNode parent, JsonNode? child) Nodes(string path, JsonObject? rootNode) {
            ReadOnlySpan<char> input = path.AsSpan();
            JsonNode parent = rootNode ?? [];
            JsonNode? child = parent;

            foreach (Range keyRange in input.Split(':')) {
                var key = input[keyRange].ToString();

                parent = child;
                child = parent[key];

                if (child is null) {
                    if (keyRange.End.Value == path.Length) {
                        child = null;
                        break;
                    }
                    child = new JsonObject();
                    parent[key] = child;
                }
            }
            return (parent, child);
        }

        private TOptions GetDefault() =>
            Get() ?? new TOptions();
    }
}
