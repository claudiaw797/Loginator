// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using Loginator.Application.Model;
using Loginator.Application.Option;
using Loginator.Application.Service;
using Loginator.Application.ViewModel;
using Loginator.Domain.Channel;
using Loginator.Domain.Option;
using Loginator.Domain.Service;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Loginator.UnitTests.Infrastructure.ServiceProviderExtensions;

namespace Loginator.Application.UnitTests {

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
        public void Can_register_log_processor() {
            sut.TestRegistration<ILogProcessor>();
        }

        [Test]
        public void Can_register_view_models() {
            sut.TestRegistration<LoginatorViewModel>();
            sut.TestRegistration<AboutViewModel>();
            sut.TestRegistration<ConfigurationViewModel>();
            sut.TestRegistration<ConnectionsViewModel>();
        }

        private static ServiceProvider Sut() {
            var services = new ServiceCollection();
            services.AddTransient(sp => A.Fake<ILogger<LoginatorViewModel>>());
            services.AddTransient(sp => A.Fake<ILogger<ConnectionsViewModel>>());
            services.AddTransient(sp => A.Fake<IOptionsMonitor<ApplicationOptions>>());
            services.AddTransient(sp => A.Fake<IOptionsRepository<ApplicationOptions>>());
            services.AddTransient(sp => A.Fake<IOptionsRepository<ConnectionsOptions>>());
            services.AddTransient(sp => A.Fake<ILogService>());
            services.AddTransient(sp => A.Fake<IStopwatchFactory>());
            services.AddTransient(sp => A.Fake<IDispatcher>());
            services.AddSingleton(sp => A.Fake<AssemblyInfo>());

            services.AddApplication();

            return services.BuildServiceProvider();
        }
    }
}