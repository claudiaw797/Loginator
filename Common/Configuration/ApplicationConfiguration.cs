
using Microsoft.Extensions.Configuration;

namespace Common.Configuration {

    public sealed class ApplicationConfiguration {

        public const string SectionName = "AppSettings";

        public bool IsMessageTraceEnabled { get; set; }
        public bool IsTimingTraceEnabled { get; set; }
    }

    public static class ApplicationConfigurationExtensions {

        public static ApplicationConfiguration GetAppSettings(this IConfiguration configuration)
            => configuration.GetSection(ApplicationConfiguration.SectionName).Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
    }
}
