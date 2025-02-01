// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FakeItEasy.Configuration;
using FakeItEasy.Core;
using FluentAssertions;
using Loginator.Application.Option;
using Loginator.Application.ViewModel;
using Loginator.Domain.Channel;
using Loginator.Domain.Option;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Loginator.Application.UnitTests.ViewModel.TestData;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents unit tests for <see cref="ConnectionsViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ConnectionsViewModelTests {

        private const string VALID_IP_ADDRESS = "140.82.121.3";
        private static readonly Connection DefaultConnection = Connection();

        private readonly SutFactory sutFactory = new();
        private readonly ConnectionsViewModel sut;

        public ConnectionsViewModelTests() {
            sut = sutFactory.Create();
        }

        [TearDown]
        public async Task TearDown() {
            await sut.DisposeAsync().ConfigureAwait(false);
        }

        [Test]
        public void Can_create_sut_without_connections() {
            var sut = sutFactory.Create();

            sut.Connections.Should()
                .BeEmpty();
        }

        [Test]
        public void Can_create_sut_with_existing_connections() {
            var testConnections = ArrangeConnections();

            var sut = sutFactory.Create();

            sut.Connections.Select(vm => vm.Connection).Should()
                .BeEquivalentTo(testConnections.Select(t => t.Connection));
            SutFactory.Dispose(testConnections);
        }

        [Test]
        public async Task Can_save_connections_when_disposed() {
            var testConnections = ArrangeConnections();
            var sut = sutFactory.Create();

            await sut.DisposeAsync().ConfigureAwait(false);

            sutFactory.AssertConnectionsOptions(testConnections);
            sutFactory.CallToLogServiceDispose().MustHaveHappened();
            SutFactory.Dispose(testConnections);
        }

        [Test]
        public void Can_determine_port_availability_when_no_connection_exists() {
            sut.IsAvailablePort(TEST_PORT).Should().BeTrue();
        }

        [Test]
        public void Can_determine_port_availability_when_connections_exist() {
            var testConnections = ArrangeConnections();

            var sut = sutFactory.Create();

            foreach (var test in testConnections) {
                sut.IsAvailablePort(test.Connection.Port).Should().BeFalse();
                sut.IsAvailablePort(test.Connection.Port + 10).Should().BeTrue();
            }
            SutFactory.Dispose(testConnections);
        }

        [Test]
        public void Can_add_connection() {
            sut.AddConnection(DefaultConnection);

            sutFactory.CallToLogServiceCreateWriter().MustHaveHappenedOnceExactly();
            sut.Connections.Select(vm => vm.Connection).Should().BeEquivalentTo([DefaultConnection]);
        }

        [Test]
        public void Can_start_log_writer() {
            var logWriter = A.Fake<ILogWriter>();

            sut.StartAsync(logWriter);

            sutFactory.CallToLogServiceStartWriter(logWriter).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Can_stop_log_writer() {
            var logWriter = A.Fake<ILogWriter>();

            sut.StopAsync(logWriter);

            sutFactory.CallToLogServiceStopWriter(logWriter).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Can_remove_connection() {
            var testConnections = ArrangeConnections();
            var sut = sutFactory.Create();

            foreach (var connectionViewModel in sut.Connections.ToArray()) {
                var test = testConnections.Single(t => t.Connection == connectionViewModel.Connection);
                await sut.RemoveAsync(connectionViewModel, test.LogWriter).ConfigureAwait(false);

                test.CallToLogServiceRemoveWriter().MustHaveHappenedOnceExactly();
                sut.Connections.Should().NotContain(connectionViewModel);
            }
            SutFactory.Dispose(testConnections);
        }

        private TestConnection[] ArrangeConnections() {
            var connections = new Connection[] {
                Connection(ConnectionType.Udp, LogType.Log4j, TEST_PORT + 1, VALID_IP_ADDRESS, ConnectionState.Stopped),
                Connection(ConnectionType.Tcp, LogType.Logcat, TEST_PORT + 2, null, ConnectionState.Running),
                Connection(ConnectionType.Udp, LogType.Logcat, TEST_PORT + 3, "127.0.0.1", ConnectionState.Running)
            };
            return sutFactory.Arrange(connections);
        }

        private class SutFactory {

            private readonly IOptionsRepository<ConnectionsOptions> optionsRepository;
            private readonly ILogService logService;
            private readonly ILogWriter logWriter;
            private readonly ILogger<ConnectionsViewModel> logger;
            private readonly ConnectionsOptions connectionsOptions = [];

            public SutFactory() {
                optionsRepository = A.Fake<IOptionsRepository<ConnectionsOptions>>();
                CallToOptionsRepositoryGet().Returns(connectionsOptions);
                CallToOptionsRepositorySave().Invokes(OnOptionsRepositorySave);

                logService = A.Fake<ILogService>();
                logWriter = A.Fake<ILogWriter>();
                CallToLogWriterConnection().Returns(DefaultConnection);
                CallToLogServiceCreateWriter().Returns(logWriter);

                logger = A.Fake<ILogger<ConnectionsViewModel>>();
            }

            public TestConnection[] Arrange(IEnumerable<Connection> connections) {
                connectionsOptions.Clear();
                connectionsOptions.AddRange(connections);

                return connections.Reverse().Select(c => new TestConnection(c, logService)).ToArray();
            }

            public ConnectionsViewModel Create() =>
                new(optionsRepository, logService, logger);


            public void AssertConnectionsOptions(IEnumerable<TestConnection> testConnections) {
                connectionsOptions.Should()
                    .BeEquivalentTo(testConnections.Select(t => t.Connection));
            }

            public static void Dispose(IEnumerable<TestConnection> testConnections) {
                foreach (var testConnection in testConnections) {
                    testConnection.Dispose();
                }
            }

            public IReturnValueArgumentValidationConfiguration<ILogWriter> CallToLogServiceCreateWriter(Connection? connection = null) =>
                A.CallTo(() => logService.CreateWriter(connection ?? DefaultConnection));

            public IReturnValueArgumentValidationConfiguration<Task> CallToLogServiceStartWriter(ILogWriter logWriter) =>
                A.CallTo(() => logService.StartWriterAsync(logWriter));

            public IReturnValueArgumentValidationConfiguration<Task> CallToLogServiceStopWriter(ILogWriter logWriter) =>
                A.CallTo(() => logService.StopWriterAsync(logWriter));

            public IReturnValueArgumentValidationConfiguration<ValueTask> CallToLogServiceDispose() =>
                A.CallTo(() => logService.DisposeAsync());

            private IReturnValueArgumentValidationConfiguration<ConnectionsOptions> CallToOptionsRepositoryGet() =>
                A.CallTo(() => optionsRepository.Get());

            private IVoidArgumentValidationConfiguration CallToOptionsRepositorySave() =>
                A.CallTo(() => optionsRepository.Save(A<Action<ConnectionsOptions>>._));

            private IReturnValueArgumentValidationConfiguration<Connection> CallToLogWriterConnection() =>
                A.CallTo(() => logWriter.Connection);

            private void OnOptionsRepositorySave(IFakeObjectCall call) {
                var changeMethod = call.Arguments.Get<Action<ConnectionsOptions>>(0);
                changeMethod?.Invoke(connectionsOptions);
            }
        }
    }
}