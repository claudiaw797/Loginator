// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FakeItEasy.Configuration;
using FluentAssertions;
using Loginator.Application.Option;
using Loginator.Application.ViewModel;
using Loginator.Domain.Channel;
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

            sut.Pause();

            sut.StateShouldBe(ConnectionState.Paused);
            sut.LogWriterActive.Should().BeFalse();
            sut.LogWriterTaskRunning = false;
        }

        [Test]
        public void Can_resume() {
            sut.LogWriterTaskRunning = true;
            sut.LogWriterActive.Should().BeFalse();
            sut.CanResume.Should().BeTrue();

            sut.Resume();

            sut.StateShouldBe(ConnectionState.Running);
            sut.LogWriterActive.Should().BeTrue();
            sut.LogWriterTaskRunning = false;
        }

        [Test]
        public void Can_start() {
            sut.LogWriterTaskRunning = false;
            sut.LogWriterActive.Should().BeFalse();
            sut.CanStart.Should().BeTrue();

            sut.Start();

            sut.StateShouldBe(ConnectionState.Running);
            sut.LogWriterActive.Should().BeTrue();
            sut.LogWriterTaskRunning = false;
        }

        [Test]
        public void Can_stop() {
            sut.LogWriterTaskRunning = true;
            sut.CanStop.Should().BeTrue();

            sut.Stop();

            sut.StateShouldBe(ConnectionState.Stopped);
            sut.LogWriterTaskRunning.Should().BeFalse();
        }

        [Test]
        public void Can_remove_from_state_stopped() {
            sut.Stop();
            sut.StateShouldBe(ConnectionState.Stopped);

            TestRemove();
        }

        [Test]
        public void Can_remove_from_state_paused() {
            sut.Start();
            sut.Pause();
            sut.StateShouldBe(ConnectionState.Paused);

            TestRemove();
        }

        [Test]
        public void Can_remove_from_state_running() {
            sut.Start();
            sut.StateShouldBe(ConnectionState.Running);

            TestRemove();
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

        private void TestRemove() {
            sut.CanRemove.Should().BeTrue();

            sut.Remove();

            sut.HasCallToLogServiceRemoveWriter();
            sut.IsConnectionsEmpty.Should().BeTrue();
        }

        private class Sut : IAsyncDisposable {

            private readonly ILogService logService;
            private readonly ILogWriter logWriter;
            private readonly Connection connection;
            private readonly ConnectionsViewModel connectionsViewModel;
            private TaskCompletionSource? tcs;

            private readonly ConnectionViewModel sut;

            public Sut(Connection connection) {
                this.connection = connection;

                logService = A.Fake<ILogService>();
                logWriter = A.Fake<ILogWriter>();

                CallToLogWriterConnection().Returns(connection);
                CallToLogWriterTask().ReturnsLazily(c => tcs is null ? Task.CompletedTask : tcs.Task);

                CallToLogServiceCreateWriter().Returns(logWriter);
                CallToLogServiceStartWriter().Invokes(c => tcs = new TaskCompletionSource());
                CallToLogServiceStopWriter().Invokes(c => tcs?.SetCanceled());

                var optionsRepository = A.Fake<IOptionsRepository<ConnectionsOptions>>();
                A.CallTo(() => optionsRepository.Get()).Returns([connection]);

                var logger = A.Fake<ILogger<ConnectionsViewModel>>();

                connectionsViewModel = new ConnectionsViewModel(optionsRepository, logService, logger);
                sut = connectionsViewModel.Connections.First();
            }

            public bool CanPause => sut.PauseCommand.CanExecute(null);
            public bool CanResume => sut.ResumeCommand.CanExecute(null);
            public bool CanStart => sut.StartCommand.CanExecute(null);
            public bool CanStop => sut.StopCommand.CanExecute(null);
            public bool CanRemove => sut.RemoveCommand.CanExecute(null);

            public bool IsConnectionsEmpty =>
                connectionsViewModel.Connections.Count == 0;

            public bool LogWriterActive {
                get { return logWriter.IsActive; }
                set { logWriter.IsActive = value; }
            }

            public bool LogWriterTaskRunning {
                get => !logWriter.Task.IsCompleted;
                set {
                    if (value)
                        tcs = new TaskCompletionSource();
                    else if (tcs is not null && !tcs.Task.IsCompleted)
                        tcs?.SetCanceled();
                }
            }

            public async ValueTask DisposeAsync() {
                await connectionsViewModel.DisposeAsync();
                LogWriterTaskRunning = false;
            }

            public void HasCallToLogServiceRemoveWriter() =>
                CallToLogServiceRemoveWriter().MustHaveHappened();

            public void StateShouldBe(ConnectionState connectionState) =>
                sut.State.Should().Be(connectionState);

            public void Pause() => sut.PauseCommand.Execute(null);
            public void Resume() => sut.ResumeCommand.Execute(null);
            public void Start() => sut.StartCommand.Execute(null);
            public void Stop() => sut.StopCommand.Execute(null);
            public void Remove() => sut.RemoveCommand.Execute(null);

            public void AssertConnection() {
                sut.ConnectionType.Should().Be(connection.ConnectionType);
                sut.LogType.Should().Be(connection.LogType);
                sut.IpAddress.Should().Be(connection.IpAddress);
                sut.Port.Should().Be(connection.Port);
                sut.DesiredState.Should().Be(connection.State);
                sut.Connection.Should().Be(connection);
            }

            public override string ToString() => sut.ToString();

            private IReturnValueArgumentValidationConfiguration<Connection> CallToLogWriterConnection() =>
                A.CallTo(() => logWriter.Connection);

            private IReturnValueArgumentValidationConfiguration<Task> CallToLogWriterTask() =>
                A.CallTo(() => logWriter.Task);

            private IReturnValueArgumentValidationConfiguration<ILogWriter> CallToLogServiceCreateWriter() =>
                A.CallTo(() => logService.CreateWriter(connection));

            private IVoidArgumentValidationConfiguration CallToLogServiceStartWriter() =>
                A.CallTo(() => logService.StartWriter(logWriter));

            private IVoidArgumentValidationConfiguration CallToLogServiceStopWriter() =>
                A.CallTo(() => logService.StopWriter(logWriter));

            private IVoidArgumentValidationConfiguration CallToLogServiceRemoveWriter() =>
                A.CallTo(() => logService.RemoveWriter(logWriter));
        }
    }
}