// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Loginator.Infrastructure.Channel;
using Loginator.UnitTests.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using static Loginator.Infrastructure.UnitTests.Channel.TestData;

namespace Loginator.Infrastructure.UnitTests.Channel {

    /// <summary>
    /// Represents unit tests for <see cref="LogService"/>.
    /// </summary>
    public class LogServiceTests {

        private readonly LogListener logListener = new();
        private readonly LogAsyncEnumerableQueueReader logQueueReader = new();

        private readonly LogService sut;

        public LogServiceTests() {
            sut = Sut();
        }

        [OneTimeTearDown]
        public async Task TearDown() {
            await sut.DisposeAsync().ConfigureAwait(false);
        }

        [Test]
        public void Can_create_and_cache_log_writer_for_connection() {
            var expectedConnection = new Connection {
                ConnectionType = ConnectionType.Udp,
                LogType = LogType.Log4j,
                Port = 7081
            };

            var actual = sut.CreateWriter(expectedConnection);

            actual.Connection.Should().Be(expectedConnection);
        }

        [Test]
        public async Task Can_write_logs_to_channel_and_stop_writer() {
            var expectedItems = TestLogs();
            var actual = sut.CreateWriter(new Connection());

            sut.StartWriter(actual);
            await logQueueReader.AddItemsAsync(expectedItems).ConfigureAwait(false);

            var t = Task.Run(() => {
                sut.StopWriter(actual);
                sut.RemoveWriter(actual);
            });

            logQueueReader.ReceivedLogs.Should().BeEquivalentTo(expectedItems, c => c.WithStrictOrdering());
            await t.ConfigureAwait(false);
        }

        private LogService Sut() {
            var logRepository = A.Fake<ILogRepository>();
            A.CallTo(() => logRepository.GetEnumerableAsync(A<int>._, A<string>._, A<CancellationToken>._)).Returns(logQueueReader.IncomingLogs);
            A.CallTo(() => logRepository.IsActive).Returns(true);

            var logRepositoryFactory = A.Fake<ILogRepositoryFactory>();
            A.CallTo(() => logRepositoryFactory.CreateLogRepository(A<ConnectionType>._, A<LogType>._)).Returns(logRepository);

            var logger = logListener.Setup<LogService>();
            var sut = new LogService(logQueueReader, logRepositoryFactory, logQueueReader.TimeProvider, logger);

            return sut;
        }
    }
}