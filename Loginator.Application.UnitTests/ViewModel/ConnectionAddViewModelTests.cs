// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FakeItEasy.Configuration;
using FluentAssertions;
using Loginator.Application.Option;
using Loginator.Application.ViewModel;
using Loginator.Domain.Channel;
using Loginator.Domain.Option;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static Loginator.UnitTests.Infrastructure.DelegateHandlerExtensions;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents unit tests for <see cref="ConnectionAddViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ConnectionAddViewModelTests {

        private const string VALID_IP_ADDRESS = "140.82.121.3";
        private const int DEFAULT_PORT = 7071;
        private static readonly Exception TestException = new InvalidOperationException("test error");

        private readonly ILogService logService;
        private readonly ILogWriter logWriter;
        private readonly ILogger<ConnectionsViewModel> logger;
        private readonly IDelegateHandler delegateHandler;
        private readonly ConnectionsViewModel connectionsViewModel;

        private readonly ConnectionAddViewModel sut;

        public ConnectionAddViewModelTests() {
            logService = A.Fake<ILogService>();
            logWriter = A.Fake<ILogWriter>();
            logger = A.Fake<ILogger<ConnectionsViewModel>>();
            delegateHandler = A.Fake<IDelegateHandler>();

            var optionsRepository = A.Fake<IOptionsRepository<ConnectionsOptions>>();
            connectionsViewModel = new ConnectionsViewModel(optionsRepository, logService, logger);

            sut = Sut();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown() {
            await connectionsViewModel.DisposeAsync().ConfigureAwait(false);
            await logService.DisposeAsync().ConfigureAwait(false);
        }

        [Test]
        public void Can_call_close_handler_without_saving_changes() {
            TestCancelChanges();

            delegateHandler.CallToClose().MustHaveHappened();
            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_do_nothing_if_no_close_handler_is_set_when_canceling() {
            sut.OnClose = null;

            TestCancelChanges();

            delegateHandler.CallToClose().MustNotHaveHappened();
            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_call_error_handler_if_close_handler_throws_when_canceling() {
            TestCloseHandlerThrowsOnCancel();

            delegateHandler.CallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Can_swallow_error_if_close_handler_throws_and_no_error_handler_is_set_when_canceling() {
            sut.OnError = null;

            TestCloseHandlerThrowsOnCancel();

            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]//, Order(1)]
        public void Can_only_execute_save_if_fields_are_initialized_to_valid_values() {
            TestFreshFields();

            sut.ConnectionType = ConnectionType.Udp;
            AssertCanSaveChanges(false);

            sut.LogType = LogType.Log4j;
            AssertCanSaveChanges(false);

            sut.Port = DEFAULT_PORT;
            AssertCanSaveChanges(true);

            sut.IpAddress = "invalid";
            AssertCanSaveChanges(false);

            sut.IpAddress = VALID_IP_ADDRESS;
            AssertCanSaveChanges(true);
        }

        [Test]
        public void Cannot_execute_save_if_port_is_already_used() {
            var connectionViewModel = new ConnectionViewModel(connectionsViewModel, logWriter, logger);
            connectionsViewModel.Connections.Add(connectionViewModel);
            TestFreshFields();

            sut.ConnectionType = ConnectionType.Udp;
            sut.LogType = LogType.Log4j;
            sut.Port = DEFAULT_PORT;
            sut.IpAddress = VALID_IP_ADDRESS;
            AssertCanSaveChanges(false);

            sut.Port += 1;
            AssertCanSaveChanges(true);
        }


        [Test]
        public void Can_save_changes_and_call_close_handler() {
            TestSaveChanges();

            delegateHandler.CallToClose().MustHaveHappened();
            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Can_save_changes_if_no_close_handler_is_set_when_saving(bool startImmediatelyValue) {
            sut.OnClose = null;

            TestSaveChanges(startImmediatelyValue: startImmediatelyValue);

            delegateHandler.CallToClose().MustNotHaveHappened();
            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Cannot_save_changes_but_call_error_handler_if_repository_throws_when_saving() {
            TestRepositoryThrowsOnSave();

            delegateHandler.CallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Cannot_save_changes_but_swallow_error_if_repository_throws_and_no_error_handler_is_set_when_saving() {
            sut.OnError = null;

            TestRepositoryThrowsOnSave();

            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_call_error_handler_if_close_handler_throws_after_saving() {
            TestCloseHandlerThrowsOnSave();

            delegateHandler.CallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Can_swallow_error_if_close_handler_throws_and_no_error_handler_is_set_when_saving() {
            sut.OnError = null;

            TestCloseHandlerThrowsOnSave();

            delegateHandler.CallToError().MustNotHaveHappened();
        }

        private void AssertCanSaveChanges(bool can) =>
            sut.AcceptChangesCommand.CanExecute(null).Should().Be(can);

        private void TestCancelChanges() {
            // Arrange
            var expected = connectionsViewModel.Connections.Count;

            // Act
            sut.CancelChangesCommand.Execute(null);

            // Assert
            connectionsViewModel.Connections.Count.Should().Be(expected);
        }

        private void TestCloseHandlerThrowsOnCancel() {
            delegateHandler.CallToClose().Throws(TestException).Once();

            TestCancelChanges();

            delegateHandler.CallToClose().MustHaveHappened();
        }

        private void TestRepositoryThrowsOnSave() {
            CallToLogServiceCreateWriter().Throws(TestException).Once();

            TestSaveChanges(assertConnectionAdded: false);

            delegateHandler.CallToClose().MustNotHaveHappened();
        }

        private void TestCloseHandlerThrowsOnSave() {
            delegateHandler.CallToClose().Throws(TestException).Once();

            TestSaveChanges();

            delegateHandler.CallToClose().MustHaveHappened();
        }

        private void TestFreshFields() {
            sut.ConnectionType.Should().Be(ConnectionType.None);
            sut.LogType.Should().Be(LogType.None);
            sut.Port.Should().Be(0);
            sut.IpAddress.Should().BeNullOrEmpty();
            AssertCanSaveChanges(false);
        }

        private void TestSaveChanges(bool startImmediatelyValue = false, bool assertConnectionAdded = true) {
            // Arrange
            sut.ConnectionType = ConnectionType.Tcp;
            sut.LogType = LogType.Logcat;
            sut.Port = DEFAULT_PORT;
            sut.IpAddress = VALID_IP_ADDRESS;
            sut.StartImmediately = startImmediatelyValue;

            var expectedCount = connectionsViewModel.Connections.Count;
            var expectedConnection = sut.ToConnection();
            CallToLogWriterConnection().Returns(expectedConnection);

            // Act
            sut.AcceptChangesCommand.Execute(null);

            // Assert
            CallToLogServiceCreateWriter().MustHaveHappened();

            if (assertConnectionAdded) {
                connectionsViewModel.Connections.Should()
                    .HaveCount(expectedCount + 1).And
                    .Contain(c => c.Connection == expectedConnection);
            }
        }

        private IReturnValueArgumentValidationConfiguration<Connection> CallToLogWriterConnection() =>
            A.CallTo(() => logWriter.Connection);

        private IReturnValueArgumentValidationConfiguration<ILogWriter> CallToLogServiceCreateWriter() =>
            A.CallTo(() => logService.CreateWriter(A<Connection>._));

        private ConnectionAddViewModel Sut() {
            var connection = new Connection {
                Port = DEFAULT_PORT,
                State = ConnectionState.Stopped
            };
            CallToLogWriterConnection().Returns(connection);
            CallToLogServiceCreateWriter().Returns(logWriter);

            return new ConnectionAddViewModel(connectionsViewModel) {
                OnClose = delegateHandler.Close,
                OnError = delegateHandler.Error
            };
        }
    }
}