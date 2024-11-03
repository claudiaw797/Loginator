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
        private readonly string section;
        private readonly string physicalPath;

        public OptionsRepository(
            IHostEnvironment environment,
            IOptionsMonitor<TOptions> optionsMonitor,
            IConfigurationRoot configuration,
            string section,
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
            var jsonFile = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(physicalPath));
            // get section: deserialized from file object, current configuration value or newly created
            var sectionObject = jsonFile is null || !jsonFile.TryGetPropertyValue(this.section, out var section)
                ? Get() ?? new TOptions()
                : JsonSerializer.Deserialize<TOptions>(section!.ToString());

            // cannot continue without section
            if (sectionObject is null) return;

            // apply changes to section
            applyChanges(sectionObject);

            // if there was no json object from file so far, create empty
            jsonFile ??= [];
            // serialize section to json and insert into file json
            jsonFile[this.section] = JsonObject.Parse(JsonSerializer.Serialize(sectionObject));
            // serialize file json and write to file
            File.WriteAllText(physicalPath, JsonSerializer.Serialize(jsonFile, serializerOptions));

            configuration.Reload();
        }

        public IDisposable? OnChanged(Action<TOptions, string?> listener) =>
            optionsMonitor.OnChange(listener);
    }
}
