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
using System.Collections.Generic;
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

        private readonly ConnectionAddViewModel sut;
        private readonly SutService sutService = new();

        public ConnectionAddViewModelTests() {
            sut = sutService.Sut;
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown() {
            await sutService.DisposeAsync().ConfigureAwait(false);
        }

        [Test]
        public void Can_call_close_handler_without_saving_changes() {
            TestCancelChanges();

            sutService.DelegateCallToClose().MustHaveHappened();
            sutService.DelegateCallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_do_nothing_if_no_close_handler_is_set_when_canceling() {
            sut.OnClose = null;

            TestCancelChanges();

            sutService.DelegateCallToClose().MustNotHaveHappened();
            sutService.DelegateCallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_call_error_handler_if_close_handler_throws_when_canceling() {
            TestCloseHandlerThrowsOnCancel();

            sutService.DelegateCallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Can_swallow_error_if_close_handler_throws_and_no_error_handler_is_set_when_canceling() {
            sut.OnError = null;

            TestCloseHandlerThrowsOnCancel();

            sutService.DelegateCallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_only_execute_save_if_fields_are_initialized_to_valid_values() {
            TestFreshFields();

            sut.ConnectionType = ConnectionType.Udp;
            AssertCanSaveChanges(false);

            sut.LogType = LogType.Log4j;
            AssertCanSaveChanges(false);

            sut.Port = DEFAULT_PORT + 1;
            AssertCanSaveChanges(true);

            sut.IpAddress = "invalid";
            AssertCanSaveChanges(false);

            sut.IpAddress = VALID_IP_ADDRESS;
            AssertCanSaveChanges(true);
        }

        [Test]
        public void Cannot_execute_save_if_port_is_already_used() {
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

            sutService.DelegateCallToClose().MustHaveHappened();
            sutService.DelegateCallToError().MustNotHaveHappened();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Can_save_changes_if_no_close_handler_is_set_when_saving(bool startImmediatelyValue) {
            sut.OnClose = null;

            TestSaveChanges(startImmediatelyValue: startImmediatelyValue);

            sutService.DelegateCallToClose().MustNotHaveHappened();
            sutService.DelegateCallToError().MustNotHaveHappened();
        }

        [Test]
        public void Cannot_save_changes_but_call_error_handler_if_repository_throws_when_saving() {
            TestRepositoryThrowsOnSave();

            sutService.DelegateCallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Cannot_save_changes_but_swallow_error_if_repository_throws_and_no_error_handler_is_set_when_saving() {
            sut.OnError = null;

            TestRepositoryThrowsOnSave();

            sutService.DelegateCallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_call_error_handler_if_close_handler_throws_after_saving() {
            TestCloseHandlerThrowsOnSave();

            sutService.DelegateCallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Can_swallow_error_if_close_handler_throws_and_no_error_handler_is_set_when_saving() {
            sut.OnError = null;

            TestCloseHandlerThrowsOnSave();

            sutService.DelegateCallToError().MustNotHaveHappened();
        }

        private void AssertCanSaveChanges(bool can) =>
            sut.AcceptChangesCommand.CanExecute(null).Should().Be(can);

        private void TestCancelChanges() {
            var expected = sutService.Connections.Count;

            sut.CancelChangesCommand.Execute(null);

            sutService.Connections.Count.Should().Be(expected);
        }

        private void TestCloseHandlerThrowsOnCancel() {
            sutService.DelegateCallToClose().Throws(TestException).Once();

            TestCancelChanges();

            sutService.DelegateCallToClose().MustHaveHappened();
        }

        private void TestRepositoryThrowsOnSave() {
            TestSaveChanges(assertConnectionAdded: false);

            sutService.DelegateCallToClose().MustNotHaveHappened();
        }

        private void TestCloseHandlerThrowsOnSave() {
            sutService.DelegateCallToClose().Throws(TestException).Once();

            TestSaveChanges();

            sutService.DelegateCallToClose().MustHaveHappened();
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
            sut.Port = DEFAULT_PORT + 1;
            sut.IpAddress = VALID_IP_ADDRESS;
            sut.StartImmediately = startImmediatelyValue;

            var expectedCount = sutService.Connections.Count;
            var expectedConnection = sut.ToConnection();
            var testConnection = sutService.Arrange(expectedConnection, !assertConnectionAdded);

            // Act
            sut.AcceptChangesCommand.Execute(null);

            // Assert
            testConnection.CallToLogServiceCreateWriter().MustHaveHappened();

            if (assertConnectionAdded) {
                sutService.Connections.Should()
                    .HaveCount(expectedCount + 1).And
                    .Contain(c => c.Connection == expectedConnection);
            }
        }

        private class SutService : IAsyncDisposable {

            private readonly ILogService logService;
            private readonly IDelegateHandler delegateHandler;
            private readonly TestConnection testConnection;
            private readonly ConnectionsViewModel connectionsViewModel;

            public SutService() {
                var connection = new Connection {
                    Port = DEFAULT_PORT,
                    State = ConnectionState.Stopped
                };
                var connectionsOptions = new ConnectionsOptions([connection]);
                var optionsRepository = A.Fake<IOptionsRepository<ConnectionsOptions>>();
                A.CallTo(() => optionsRepository.Get()).Returns(connectionsOptions);

                logService = A.Fake<ILogService>();
                testConnection = new(connection, logService);
                var logger = A.Fake<ILogger<ConnectionsViewModel>>();
                connectionsViewModel = new ConnectionsViewModel(optionsRepository, logService, logger);

                delegateHandler = A.Fake<IDelegateHandler>();
                Sut = new ConnectionAddViewModel(connectionsViewModel) {
                    OnClose = delegateHandler.Close,
                    OnError = delegateHandler.Error
                };
            }

            public IReadOnlyCollection<ConnectionViewModel> Connections =>
                connectionsViewModel.Connections;

            public ConnectionAddViewModel Sut { get; private init; }

            public async ValueTask DisposeAsync() {
                await connectionsViewModel.DisposeAsync().ConfigureAwait(false);
                testConnection.Dispose();
            }

            public TestConnection Arrange(Connection connection, bool throwOnCreation = false) {
                var testConnection = new TestConnection(connection, logService);
                if (throwOnCreation) {
                    CallToLogServiceCreateWriter().Throws(TestException);
                }
                return testConnection;
            }

            public IVoidArgumentValidationConfiguration DelegateCallToClose() =>
                delegateHandler.CallToClose();

            public IVoidArgumentValidationConfiguration DelegateCallToError(Exception? expected = null) =>
                delegateHandler.CallToError(expected);

            private IReturnValueArgumentValidationConfiguration<ILogWriter> CallToLogServiceCreateWriter() =>
                A.CallTo(() => logService.CreateWriter(A<Connection>._));
        }
    }
}