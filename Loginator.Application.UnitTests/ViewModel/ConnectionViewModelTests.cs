// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Application.Option;
using Loginator.Application.ViewModel;
using Loginator.Domain.Option;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Loginator.Application.UnitTests.ViewModel.TestData;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents unit tests for <see cref="ConnectionViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ConnectionViewModelTests {

        private static readonly Connection DefaultConnection =
            Connection(ConnectionType.Tcp, LogType.Logcat, 1008, "140.82.121.3", ConnectionState.Stopped);

        private readonly Sut sut;

        public ConnectionViewModelTests() {
            sut = new(DefaultConnection);
        }

        [TearDown]
        public async Task TearDown() {
            await sut.DisposeAsync();
        }

        [Test]
        public void Can_determine_connection_values() {
            sut.AssertConnection();
        }

        [Test]
        public void Can_display_its_type_and_port_in_string_representation() {
            var actual = sut.ToString();

            actual
                .Should().Contain($"{DefaultConnection.ConnectionType}")
                .And.Contain($"{DefaultConnection.Port}");
        }

        [Test]
        public void Can_determine_state_from_log_writer() {
            sut.LogWriterActive.Should().BeFalse();
            sut.StateShouldBe(ConnectionState.Stopped);

            sut.LogWriterTaskRunning = true;
            sut.StateShouldBe(ConnectionState.Paused);

            sut.LogWriterActive = true;
            sut.StateShouldBe(ConnectionState.Running);

            sut.LogWriterTaskRunning = false;
            sut.StateShouldBe(ConnectionState.Stopped);
        }

        [Test]
        public void Can_determine_if_can_pause() {
            sut.LogWriterActive.Should().BeFalse();
            sut.CanPause.Should().BeFalse();

            sut.LogWriterTaskRunning = true;
            sut.CanPause.Should().BeFalse();

            sut.LogWriterActive = true;
            sut.CanPause.Should().BeTrue();

            sut.LogWriterTaskRunning = false;
            sut.CanPause.Should().BeFalse();
        }

        [Test]
        public void Can_determine_if_can_resume() {
            sut.LogWriterActive.Should().BeFalse();
            sut.CanResume.Should().BeFalse();

            sut.LogWriterTaskRunning = true;
            sut.CanResume.Should().BeTrue();

            sut.LogWriterActive = true;
            sut.CanResume.Should().BeFalse();

            sut.LogWriterTaskRunning = false;
            sut.CanResume.Should().BeFalse();
        }

        [Test]
        public void Can_determine_if_can_start() {
            sut.LogWriterActive.Should().BeFalse();
            sut.CanStart.Should().BeTrue();

            sut.LogWriterTaskRunning = true;
            sut.CanStart.Should().BeFalse();

            sut.LogWriterActive = true;
            sut.CanStart.Should().BeFalse();

            sut.LogWriterTaskRunning = false;
            sut.CanStart.Should().BeTrue();
        }

        [Test]
        public void Can_determine_if_can_stop() {
            sut.LogWriterActive.Should().BeFalse();
            sut.CanStop.Should().BeFalse();

            sut.LogWriterTaskRunning = true;
            sut.CanStop.Should().BeTrue();

            sut.LogWriterActive = true;
            sut.CanStop.Should().BeTrue();

            sut.LogWriterTaskRunning = false;
            sut.CanStop.Should().BeFalse();
        }

        [Test]
        public void Can_determine_if_can_remove() {
            sut.LogWriterActive.Should().BeFalse();
            sut.CanRemove.Should().BeTrue();

            sut.LogWriterTaskRunning = true;
            sut.CanRemove.Should().BeTrue();

            sut.LogWriterActive = true;
            sut.CanRemove.Should().BeTrue();

            sut.LogWriterTaskRunning = false;
            sut.CanRemove.Should().BeTrue();
        }

        [Test]
        public void Can_pause() {
            sut.LogWriterTaskRunning = true;
            sut.LogWriterActive = true;
            sut.CanPause.Should().BeTrue();

            sut.PauseAsync();

            sut.StateShouldBe(ConnectionState.Paused);
            sut.LogWriterActive.Should().BeFalse();
            sut.LogWriterTaskRunning = false;
        }

        [Test]
        public void Can_resume() {
            sut.LogWriterTaskRunning = true;
            sut.LogWriterActive.Should().BeFalse();
            sut.CanResume.Should().BeTrue();

            sut.ResumeAsync();

            sut.StateShouldBe(ConnectionState.Running);
            sut.LogWriterActive.Should().BeTrue();
            sut.LogWriterTaskRunning = false;
        }

        [Test]
        public void Can_start() {
            sut.LogWriterTaskRunning = false;
            sut.LogWriterActive.Should().BeFalse();
            sut.CanStart.Should().BeTrue();

            sut.StartAsync();

            sut.StateShouldBe(ConnectionState.Running);
            sut.LogWriterActive.Should().BeTrue();
            sut.LogWriterTaskRunning = false;
        }

        [Test]
        public void Can_stop() {
            sut.LogWriterTaskRunning = true;
            sut.CanStop.Should().BeTrue();

            sut.StopAsync();

            sut.StateShouldBe(ConnectionState.Stopped);
            sut.LogWriterTaskRunning.Should().BeFalse();
        }

        [Test]
        public async Task Can_remove_from_state_stopped() {
            await sut.StopAsync().ConfigureAwait(false);
            sut.StateShouldBe(ConnectionState.Stopped);

            await TestRemoveAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task Can_remove_from_state_paused() {
            await sut.StartAsync().ConfigureAwait(false);
            await sut.PauseAsync().ConfigureAwait(false);
            sut.StateShouldBe(ConnectionState.Paused);

            await TestRemoveAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task Can_remove_from_state_running() {
            await sut.StartAsync().ConfigureAwait(false);
            sut.StateShouldBe(ConnectionState.Running);

            await TestRemoveAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task Can_be_created_from_state_stopped() {
            await using var sut = new Sut(Connection(state: ConnectionState.Stopped));

            sut.CanPause.Should().BeFalse();
            sut.CanResume.Should().BeFalse();
            sut.CanStart.Should().BeTrue();
            sut.CanStop.Should().BeFalse();
            sut.CanRemove.Should().BeTrue();
        }

        [Test]
        public async Task Can_be_created_from_state_paused_resulting_in_stopped() {
            await using var sut = new Sut(Connection(state: ConnectionState.Paused));

            sut.CanPause.Should().BeFalse();
            sut.CanResume.Should().BeFalse();
            sut.CanStart.Should().BeTrue();
            sut.CanStop.Should().BeFalse();
            sut.CanRemove.Should().BeTrue();
        }

        [Test]
        public async Task Can_be_created_from_state_running() {
            await using var sut = new Sut(Connection(state: ConnectionState.Running));

            sut.CanPause.Should().BeTrue();
            sut.CanResume.Should().BeFalse();
            sut.CanStart.Should().BeFalse();
            sut.CanStop.Should().BeTrue();
            sut.CanRemove.Should().BeTrue();
        }

        private async Task TestRemoveAsync() {
            sut.CanRemove.Should().BeTrue();

            await sut.RemoveAsync().ConfigureAwait(false);

            sut.HasCallToLogServiceRemoveWriter();
            sut.IsConnectionsEmpty.Should().BeTrue();
        }

        private class Sut : TestConnection, IAsyncDisposable {

            private readonly ConnectionsViewModel connectionsViewModel;
            private readonly ConnectionViewModel sut;

            public Sut(Connection connection) : base(connection) {
                var optionsRepository = A.Fake<IOptionsRepository<ConnectionsOptions>>();
                A.CallTo(() => optionsRepository.Get()).Returns([connection]);

                var logger = A.Fake<ILogger<ConnectionsViewModel>>();

                connectionsViewModel = new ConnectionsViewModel(optionsRepository, logService, logger);
                sut = connectionsViewModel.Connections.First();
            }

            public bool CanPause => sut.PauseAsyncCommand.CanExecute(null);
            public bool CanResume => sut.ResumeAsyncCommand.CanExecute(null);
            public bool CanStart => sut.StartAsyncCommand.CanExecute(null);
            public bool CanStop => sut.StopAsyncCommand.CanExecute(null);
            public bool CanRemove => sut.RemoveAsyncCommand.CanExecute(null);

            public bool IsConnectionsEmpty =>
                connectionsViewModel.Connections.Count == 0;

            public bool LogWriterActive {
                get { return LogWriter.IsActive; }
                set { LogWriter.IsActive = value; }
            }

            public async ValueTask DisposeAsync() {
                await connectionsViewModel.DisposeAsync();
                LogWriterTaskRunning = false;
            }

            public void HasCallToLogServiceRemoveWriter() =>
                CallToLogServiceRemoveWriter().MustHaveHappened();

            public void StateShouldBe(ConnectionState connectionState) =>
                sut.State.Should().Be(connectionState);

            public Task PauseAsync() => sut.PauseAsyncCommand.ExecuteAsync(null);
            public Task ResumeAsync() => sut.ResumeAsyncCommand.ExecuteAsync(null);
            public Task StartAsync() => sut.StartAsyncCommand.ExecuteAsync(null);
            public Task StopAsync() => sut.StopAsyncCommand.ExecuteAsync(null);
            public Task RemoveAsync() => sut.RemoveAsyncCommand.ExecuteAsync(null);

            public void AssertConnection() {
                sut.ConnectionType.Should().Be(Connection.ConnectionType);
                sut.LogType.Should().Be(Connection.LogType);
                sut.IpAddress.Should().Be(Connection.IpAddress);
                sut.Port.Should().Be(Connection.Port);
                sut.State.Should().Be(Connection.State);
                sut.Connection.Should().Be(Connection);
            }

            public override string ToString() => sut.ToString();
        }
    }
}