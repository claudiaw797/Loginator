using Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Common.Bootstrapper {

    public static class DiBootstrapperCommon {

        public static void ConfigureWritable<T>(this IServiceCollection services,
            IConfigurationSection section,
            string file = "appsettings.json")
            where T : class, new() {

            services.Configure<T>(section);

            services.AddTransient<IWritableOptions<T>>(provider => {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var environment = provider.GetRequiredService<IHostEnvironment>();
                var options = provider.GetRequiredService<IOptionsMonitor<T>>();

                return new WritableOptions<T>(environment, options, (IConfigurationRoot)configuration, section.Key, file);
            });
        }
    }
}
