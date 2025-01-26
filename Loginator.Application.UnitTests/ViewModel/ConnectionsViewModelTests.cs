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
        //private const int TEST_PORT = 7071;
        private static readonly Connection DefaultConnection = Connection();

        private readonly IOptionsRepository<ConnectionsOptions> optionsRepository;
        private readonly ILogService logService;
        private readonly ILogWriter logWriter;
        private readonly ILogger<ConnectionsViewModel> logger;
        private readonly ConnectionsOptions connectionsOptions = [];

        private readonly ConnectionsViewModel sut;

        public ConnectionsViewModelTests() {
            optionsRepository = A.Fake<IOptionsRepository<ConnectionsOptions>>();
            logService = A.Fake<ILogService>();
            logWriter = A.Fake<ILogWriter>();
            logger = A.Fake<ILogger<ConnectionsViewModel>>();

            sut = Sut();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown() {
            await sut.DisposeAsync().ConfigureAwait(false);
            await logService.DisposeAsync().ConfigureAwait(false);
            await logWriter.DisposeAsync().ConfigureAwait(false);
        }

        [Test]
        public void Can_create_sut_without_connections() {
            var sut = Sut();

            sut.Connections.Should()
                .BeEmpty();
        }

        [Test]
        public void Can_create_sut_with_existing_connections() {
            var testData = ArrangeConnections();

            var sut = new ConnectionsViewModel(optionsRepository, logService, logger);

            sut.Connections.Select(vm => vm.Connection).Should()
                .BeEquivalentTo(testData.Select(t => t.Connection));
        }

        [Test]
        public async Task Can_save_connections_when_disposed() {
            var testData = ArrangeConnections();
            var sut = new ConnectionsViewModel(optionsRepository, logService, logger);

            await sut.DisposeAsync().ConfigureAwait(false);

            connectionsOptions.Should()
                .BeEquivalentTo(testData.Select(t => t.Connection));
            A.CallTo(() => logService.DisposeAsync()).MustHaveHappened();
        }

        [Test]
        public void Can_determine_port_availability_when_no_connection_exists() {
            sut.IsAvailablePort(TEST_PORT).Should().BeTrue();
        }

        [Test]
        public void Can_determine_port_availability_when_connections_exist() {
            var testData = ArrangeConnections();

            var sut = new ConnectionsViewModel(optionsRepository, logService, logger);

            foreach (var test in testData) {
                sut.IsAvailablePort(test.Connection.Port).Should().BeFalse();
                sut.IsAvailablePort(test.Connection.Port + 10).Should().BeTrue();
            }
        }

        [Test]
        public void Can_add_connection() {
            sut.AddConnection(DefaultConnection);

            CallToLogServiceCreateWriter().MustHaveHappenedOnceExactly();
            sut.Connections.Select(vm => vm.Connection).Should().BeEquivalentTo([DefaultConnection]);
        }

        [Test]
        public void Can_start_log_writer() {
            sut.Start(logWriter);

            A.CallTo(() => logService.StartWriter(logWriter)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Can_stop_log_writer() {
            sut.Stop(logWriter);

            A.CallTo(() => logService.StopWriter(logWriter)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Can_remove_connection() {
            var testData = ArrangeConnections();
            var sut = new ConnectionsViewModel(optionsRepository, logService, logger);

            foreach (var connectionViewModel in sut.Connections.ToArray()) {
                var test = testData.Single(t => t.Connection == connectionViewModel.Connection);
                sut.Remove(connectionViewModel, test.LogWriter);

                A.CallTo(() => logService.RemoveWriter(test.LogWriter)).MustHaveHappenedOnceExactly();
                sut.Connections.Should().NotContain(connectionViewModel);
            }
        }

        private IReturnValueArgumentValidationConfiguration<ConnectionsOptions> CallToOptionsRepositoryGet() =>
            A.CallTo(() => optionsRepository.Get());

        private IVoidArgumentValidationConfiguration CallToOptionsRepositorySave() =>
            A.CallTo(() => optionsRepository.Save(A<Action<ConnectionsOptions>>._));

        private IReturnValueArgumentValidationConfiguration<Connection> CallToLogWriterConnection() =>
            A.CallTo(() => logWriter.Connection);

        private IReturnValueArgumentValidationConfiguration<ILogWriter> CallToLogServiceCreateWriter(Connection? connection = null) =>
            A.CallTo(() => logService.CreateWriter(connection ?? DefaultConnection));

        private List<ConnectionData> ArrangeConnections() {
            var connections = new Connection[] {
                Connection(ConnectionType.Udp, LogType.Log4j, TEST_PORT + 1, VALID_IP_ADDRESS, ConnectionState.Stopped),
                Connection(ConnectionType.Tcp, LogType.Logcat, TEST_PORT + 2, null, ConnectionState.Paused),
                Connection(ConnectionType.Udp, LogType.Logcat, TEST_PORT + 3, "127.0.0.1", ConnectionState.Running)
            };

            var connectionOptions = new ConnectionsOptions(connections);
            CallToOptionsRepositoryGet().Returns(connectionOptions);

            var connectionData = new List<ConnectionData>();
            foreach (var connection in connections.Reverse()) {
                var logWriter = A.Fake<ILogWriter>();
                A.CallTo(() => logWriter.Connection).Returns(connection);
                CallToLogServiceCreateWriter(connection).Returns(logWriter).Once();
                connectionData.Add(new(connection, logWriter));
            }
            return connectionData;
        }

        private void OnOptionsRepositorySave(IFakeObjectCall call) {
            var changeMethod = call.Arguments.Get<Action<ConnectionsOptions>>(0);
            changeMethod?.Invoke(connectionsOptions);
        }

        private ConnectionsViewModel Sut() {
            CallToOptionsRepositoryGet().Returns(connectionsOptions);
            CallToOptionsRepositorySave().Invokes(OnOptionsRepositorySave);
            CallToLogWriterConnection().Returns(DefaultConnection);
            CallToLogServiceCreateWriter().Returns(logWriter);

            return new ConnectionsViewModel(optionsRepository, logService, logger);
        }

        public record ConnectionData(Connection Connection, ILogWriter LogWriter) { }
    }
}