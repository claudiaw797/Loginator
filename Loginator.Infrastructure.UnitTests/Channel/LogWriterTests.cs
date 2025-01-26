// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Domain.Model;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Loginator.Infrastructure.Channel;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Loginator.Infrastructure.UnitTests.Channel.TestData;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using SysChannel = System.Threading.Channels.Channel;

namespace Loginator.Infrastructure.UnitTests.Channel {

    /// <summary>
    /// Represents unit tests for <see cref="LogWriter"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class LogWriterTests {

        private readonly ILogRepository logRepository;
        private readonly ILogRepositoryFactory logRepositoryFactory;
        private readonly Channel<Log> channel;
        private readonly LogListener logListener = new();
        private readonly LogAsyncEnumerableQueueReader logQueueReader;

        private readonly LogWriter sut;

        public LogWriterTests() {
            logRepository = A.Fake<ILogRepository>();
            logRepositoryFactory = A.Fake<ILogRepositoryFactory>();
            channel = SysChannel.CreateUnbounded<Log>(new() { SingleReader = true });
            logQueueReader = new(channel.Reader);

            sut = Sut();
        }

        [TearDown]
        public async Task TearDown() {
            logQueueReader.Complete();
            await sut.DisposeAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task Can_create_sut() {
            var connection = new Connection {
                ConnectionType = ConnectionType.Udp,
                LogType = LogType.Log4j,
                Port = 7081
            };
            var logger = A.Fake<ILogger<LogService>>();

            var sut = new LogWriter(channel.Writer, connection, logger);

            sut.Connection.Should().Be(connection);
            sut.IsActive.Should().BeFalse();
            sut.Task.IsCompleted.Should().BeTrue();
            sut.Token.Should().Be(CancellationToken.None);
            await sut.DisposeAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task Can_write_logs_to_channel() {
            var expectedItems = TestLogs();
            var cts = new CancellationTokenSource();

            sut.Start(logRepositoryFactory, cts.Token);
            await logQueueReader.AddItemsAsync(expectedItems).ConfigureAwait(false);
            StopSut(cts);

            logQueueReader.ReceivedLogs.Should().BeEquivalentTo(expectedItems, c => c.WithStrictOrdering());
        }

        [TestCaseSource(typeof(ExceptionTests), nameof(ExceptionTests.TestCases))]
        public async Task Can_end_when_repository_closed(Exception expectedException, LogLevel expectedLevel, string expectedMessage) {
            A.CallTo(() => logRepository.GetEnumerableAsync(A<int>._, A<string>._, A<CancellationToken>._)).Throws(expectedException).Once();
            var cts = new CancellationTokenSource();

            sut.Start(logRepositoryFactory, cts.Token);
            await WaitForLogCallAsync(expectedLevel).ConfigureAwait(false);
            StopSut(cts);

            logQueueReader.ReceivedLogs.Should().BeEmpty();
            logListener.Contains(expectedLevel, messagePattern: $".*{expectedMessage}.*").Should().BeTrue();
        }

        [Test]
        public async Task Can_deactivate_log_repository() {
            sut.IsActive.Should().BeFalse();

            sut.Start(logRepositoryFactory, CancellationToken.None);
            await Task.Yield();
            sut.IsActive.Should().BeTrue();

            sut.IsActive = false;
            A.CallToSet(() => logRepository.IsActive).To(false).MustHaveHappened();
        }

        [Test]
        public async Task Cannot_restart_before_completed() {
            var cts = new CancellationTokenSource();
            sut.Task.IsCompleted.Should().BeTrue();

            sut.Start(logRepositoryFactory, cts.Token);
            await Task.Yield();
            sut.Task.IsCompleted.Should().BeFalse();

            sut.Start(logRepositoryFactory, cts.Token);
            await Task.Yield();
            sut.Task.IsCompleted.Should().BeFalse();

            StopSut(cts);
            logListener.Contains(LogLevel.Information, messagePattern: ".*already.*running.*").Should().BeTrue();
        }

        private void StopSut(CancellationTokenSource cts) {
            try {
                cts.Cancel(false);
                sut.Stop(cts.Token);
            }
            catch (OperationCanceledException) {
            }
        }

        private async Task WaitForLogCallAsync(LogLevel level) {
            while (true) {
                await Task.Yield();
                if (logListener.Contains(level)) break;
            }
        }

        private LogWriter Sut() {
            A.CallTo(() => logRepository.GetEnumerableAsync(A<int>._, A<string>._, A<CancellationToken>._)).Returns(logQueueReader.IncomingLogs);
            A.CallTo(() => logRepository.IsActive).Returns(true);

            A.CallTo(() => logRepositoryFactory.CreateLogRepository(A<ConnectionType>._, A<LogType>._)).Returns(logRepository);

            var logger = logListener.Setup<LogService>();
            var sut = new LogWriter(channel.Writer, new Connection(), logger);

            return sut;
        }

        private class ExceptionTests {

            public static IEnumerable TestCases {
                get {
                    var expected = "Test error";
                    yield return new TestCaseData(new ObjectDisposedException(expected), LogLevel.Warning, "closed");
                    yield return new TestCaseData(new OperationCanceledException(expected), LogLevel.Warning, expected);
                    yield return new TestCaseData(new SocketException(1, expected), LogLevel.Warning, expected);
                    yield return new TestCaseData(new InvalidOperationException(expected), LogLevel.Error, "closed");
                }
            }
        }
    }
}