// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Domain.Converter;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Loginator.Infrastructure.Converter;
using Loginator.Infrastructure.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Loginator.Infrastructure.UnitTests {

    /// <summary>
    /// Represents unit tests for <see cref="ServiceCollectionExtensions"/>.
    /// </summary>
    public class ServiceCollectionExtensionsTests {

        private static readonly ConnectionType[] ConnectionTypes = [ConnectionType.Udp, ConnectionType.Tcp];
        private static readonly LogType[] LogTypes = [LogType.Log4j, LogType.Logcat];

        private readonly IServiceProvider sut;

        public ServiceCollectionExtensionsTests() {
            sut = Sut();
        }

        [Test]
        public void Can_register_conversion_factories() {
            foreach (var logType in LogTypes) {
                TestRegistration<ILogConversionService>(logType);
            }
        }

        [Test]
        public void Can_register_sockets() {
            foreach (var connectionType in ConnectionTypes) {
                TestRegistration<AbstractSocket>(connectionType);
            }
        }

        [Test]
        public void Can_register_log_repositories() {
            foreach (var connectionType in ConnectionTypes) {
                foreach (var logType in LogTypes) {
                    TestRegistration<ILogRepository>((connectionType, logType));
                }
            }
        }

        private static IServiceProvider Sut() {
            var services = new ServiceCollection();
            services.AddTransient(sp => A.Fake<ILogger<Log4jConversionService>>());
            services.AddTransient(sp => A.Fake<ILogger<LogcatConversionService>>());
            services.AddTransient(sp => A.Fake<ILogger<LogRepository>>());
            services.AddTransient(sp => A.Fake<IOptionsMonitor<LogProcessingOptions>>());

            services.AddInfrastructure();

            return services.BuildServiceProvider();
        }

        private void TestRegistration<TService>(object? serviceKey) {
            var actual = sut.GetKeyedService<TService>(serviceKey);
            actual.Should().BeAssignableTo<TService>();
        }
    }
}