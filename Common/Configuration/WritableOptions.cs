using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Common.Configuration {

    public class WritableOptions<T> : IWritableOptions<T> where T : class, new() {

        private static readonly JsonSerializerOptions serializerOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly IOptionsMonitor<T> options;
        private readonly IConfigurationRoot configuration;
        private readonly string section;
        private readonly string physicalPath;

        public WritableOptions(
            IHostEnvironment environment,
            IOptionsMonitor<T> options,
            IConfigurationRoot configuration,
            string section,
            string file) {

            var physicalPath = environment.ContentRootFileProvider.GetFileInfo(file).PhysicalPath
                ?? throw new ArgumentException($"No file found for {file}", nameof(file));

            this.options = options;
            this.configuration = configuration;
            this.section = section;
            this.physicalPath = physicalPath;
        }

        public T Value => options.CurrentValue;

        public T Get(string name) => options.Get(name);

        public IDisposable? OnChange(Action<T, string?> listener) => options.OnChange(listener);

        public void Update(Action<T> applyChanges) {
            // create json object from current file
            var jsonFile = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(physicalPath));
            // get section: deserialized from file object, current configuration value or newly created
            var sectionObject = jsonFile is null || !jsonFile.TryGetPropertyValue(this.section, out var section)
                ? Value ?? new T()
                : JsonSerializer.Deserialize<T>(section!.ToString());

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
    }
}
