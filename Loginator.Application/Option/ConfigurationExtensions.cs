// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Option;
using Microsoft.Extensions.Configuration;

namespace Loginator.Application.Option {

    public static class ConfigurationExtensions {

        public static IConfigurationSection GetApplicationSection(this IConfiguration configuration)
            => configuration.GetSection(ApplicationOptions.SectionName);

        public static IConfigurationSection GetLogProcessingSection(this IConfiguration configuration)
            => configuration.GetSection($"{ApplicationOptions.SectionName}:{LogProcessingOptions.SectionName}");

        public static IConfigurationSection GetColorsSection(this IConfiguration configuration)
            => configuration.GetSection($"{ApplicationOptions.SectionName}:{ColorsOptions.SectionName}");

        public static ApplicationOptions GetAppSettings(this IConfiguration configuration)
            => configuration.GetApplicationSection().Get<ApplicationOptions>() ?? new();

        public static LogProcessingOptions GetMessagingSettings(this IConfiguration configuration)
            => configuration.GetLogProcessingSection().Get<LogProcessingOptions>() ?? new();

        public static ColorsOptions GetColorsSettings(this IConfiguration configuration)
            => configuration.GetColorsSection().Get<ColorsOptions>() ?? new();
    }
}
