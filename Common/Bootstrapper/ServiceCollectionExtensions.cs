// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Option;
using Loginator.Infrastructure.Option;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Loginator.Infrastructure {

    public static class ServiceCollectionExtensions {

        public static void AddWritableOptions<TOptions>(this IServiceCollection services, IConfigurationSection section, string file = "appsettings.json")
            where TOptions : class, new() {

            services.Configure<TOptions>(section);

            services.AddTransient<IOptionsRepository<TOptions>>(provider => {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var environment = provider.GetRequiredService<IHostEnvironment>();
                var options = provider.GetRequiredService<IOptionsMonitor<TOptions>>();

                return new OptionsRepository<TOptions>(environment, options, (IConfigurationRoot)configuration, section.Key, file);
            });
        }
    }
}
