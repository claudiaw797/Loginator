// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Domain.Channel;
using Loginator.Domain.Converter;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Loginator.Domain.Service;
using Loginator.Infrastructure.Channel;
using Loginator.Infrastructure.Converter;
using Loginator.Infrastructure.Server;
using Loginator.Infrastructure.Service;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Loginator.Infrastructure.ServiceCollectionExtensions;
using static Loginator.UnitTests.Infrastructure.ServiceProviderExtensions;

namespace Loginator.Infrastructure.UnitTests {

    /// <summary>
    /// Represents unit tests for <see cref="ServiceCollectionExtensions"/>.
    /// </summary>
    public class ServiceCollectionExtensionsTests {

        private readonly ServiceProvider sut;

        public ServiceCollectionExtensionsTests() {
            sut = Sut();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            sut.Dispose();
        }

        [Test]
        public void Can_register_sockets() {
            foreach (var connectionType in ConnectionTypes) {
                sut.TestRegistration<AbstractSocket>(connectionType);
            }
        }

        [Test]
        public void Can_register_log_conversion_services() {
            foreach (var logType in LogTypes) {
                sut.TestRegistration<ILogConversionService>(logType);
            }
        }

        [Test]
        public void Can_register_log_repositories() {
            var factory = sut.TestRegistration<ILogRepositoryFactory>();

            foreach (var connectionType in ConnectionTypes) {
                foreach (var logType in LogTypes) {
                    sut.TestRegistration<ILogRepository>((connectionType, logType));

                    factory.CreateLogRepository(connectionType, logType).Should().NotBeNull();
                }
            }
        }

        [Test]
        public void Can_register_log_service() {
            sut.TestRegistration<ILogService>();
        }

        [Test]
        public void Can_register_stop_watches() {
            var factory = sut.TestRegistration<IStopwatchFactory>();

            var values = new bool[] { true, false };
            foreach (var value in values) {
                sut.TestRegistration<IStopwatch>(value);

                factory.CreateStopwatch(value).Should().NotBeNull();
            }
        }

        private static ServiceProvider Sut() {
            var services = new ServiceCollection();
            services.AddTransient(sp => A.Fake<ILogger<Log4jConversionService>>());
            services.AddTransient(sp => A.Fake<ILogger<LogcatConversionService>>());
            services.AddTransient(sp => A.Fake<ILogger<LogRepository>>());
            services.AddTransient(sp => A.Fake<ILogger<LogService>>());
            services.AddTransient(sp => A.Fake<ILogger<StopwatchEnabled>>());
            services.AddTransient(sp => A.Fake<IOptionsMonitor<LogProcessingOptions>>());
            services.AddTransient(sp => A.Fake<ILogProcessor>());

            services.AddInfrastructure();

            return services.BuildServiceProvider();
        }
    }
}