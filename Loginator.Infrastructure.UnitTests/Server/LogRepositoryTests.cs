// Copyright (C) 2024 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Domain.Option;
using Loginator.Infrastructure.Converter;
using Loginator.Infrastructure.Server;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Loginator.Infrastructure.UnitTests.Server.LogRepositoryTestData;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Loginator.Infrastructure.UnitTests.Server {

    /// <summary>
    /// Represents unit tests for <see cref="LogRepository"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class LogRepositoryTests {

        private const string EMPTY_LOG = "<event></event>";
        private const string NO_LOG = "no log";
        private static readonly TimeSpan CancelTimespan = TimeSpan.FromMilliseconds(100);

        private readonly AbstractSocket socket;
        private readonly FakeSocketServer socketServer = new();
        private readonly LogListener logListener = new();
        private readonly LogRepository sut;

        public LogRepositoryTests() {
            socket = A.Fake<AbstractSocket>();
            sut = Sut();
        }

        [Test]
        public async Task Can_cancel_operation_after_reception_of_valid_logs() {
            var expectedCallCount = 5;
            socketServer.SetReturnValues(EMPTY_LOG);
            socketServer.AutoCancel = false;

            await TestCancellation(expectedCallCount);
            logListener.Contains(LogLevel.Trace, messagePattern: EMPTY_LOG, expectedTimes: expectedCallCount);
        }

        [Test]
        public async Task Can_cancel_operation_after_reception_of_invalid_logs() {
            var expectedCallCount = 0;
            socketServer.SetReturnValues(NO_LOG);
            socketServer.AutoCancel = false;
            socketServer.Cancel(CancelTimespan);

            await TestCancellation(expectedCallCount);
            logListener.Contains(LogLevel.Trace, messagePattern: NO_LOG, expectedTimes: expectedCallCount);
        }

        [Test]
        public async Task Can_cancel_operation_after_reception_of_zero_bytes() {
            var expectedCallCount = 0;
            socketServer.SetReturnValues();
            socketServer.AutoCancel = false;
            socketServer.Cancel(CancelTimespan);

            await TestCancellation(expectedCallCount);
        }

        [Test]
        public async Task Can_cancel_operation_after_waiting_without_reception() {
            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .ReturnsLazily(socketServer.WaitUntilCanceled);

            var expectedCallCount = 0;
            socketServer.Cancel(CancelTimespan);

            await TestCancellation(expectedCallCount);
        }

        [Test]
        public async Task Can_convert_valid_log4j_strings_to_logs() {
            socketServer.SetReturnValues(ValidLogMessages().ToArray());
            var comparer = new LogComparer();

            await foreach (var actual in sut.GetEnumerableAsync(0, null, socketServer.CancellationToken)) {
                actual.Should().Be(ValidLog, comparer);
            }
        }

        [Test]
        public async Task Can_deactivate_reception_of_valid_logs() {
            var expectedCallCount = 0;
            socketServer.SetReturnValues(EMPTY_LOG);
            socketServer.Cancel(CancelTimespan);
            sut.IsActive = false;

            await TestCancellation(expectedCallCount);
            logListener.Contains(LogLevel.Trace, messagePattern: EMPTY_LOG, expectedTimes: expectedCallCount);
            sut.IsActive.Should().BeFalse();
        }

        [TestCase(default)]
        [TestCase("203.0.113.195")]
        public async Task Can_listen_for_client_activity_on_network_interface(string? ipAddress) {
            var expectedPort = 1234;
            var expectedEndpoint = new IPEndPoint(ipAddress is null ? IPAddress.Any : IPAddress.Parse(ipAddress), expectedPort);
            socketServer.SetReturnValues(EMPTY_LOG, NO_LOG, EMPTY_LOG);

            await foreach (var _ in sut.GetEnumerableAsync(expectedPort, ipAddress, socketServer.CancellationToken)) {
            }
            sut.Dispose();

            A.CallTo(() => socket.Bind(An<EndPoint>.That.IsEqualTo(expectedEndpoint)))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => socket.Close())
                .MustHaveHappenedOnceExactly();
        }

        private async Task TestCancellation(int expectedCallCount) {
            var actualCallCount = 0;

            try {
                await foreach (var _ in sut.GetEnumerableAsync(0, null, socketServer.CancellationToken)) {
                    if (++actualCallCount == expectedCallCount) socketServer.Cancel();
                }
                await Task.Yield();
            }
            catch (InvalidOperationException) {
                expectedCallCount.Should().Be(0);
            }

            actualCallCount.Should().Be(expectedCallCount);
        }

        private LogRepository Sut() {
            A.CallTo(() => socket.AcceptAsync(A<CancellationToken>._))
                .Returns(socket);
            A.CallTo(() => socket.IsConnectedAsync(A<Socket>._, A<CancellationToken>._))
                .Returns(true);
            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .ReturnsLazily(socketServer.FillMemoryAndReturnLength);

            var config = new LogProcessingOptions {
                AllowAnonymousMessages = true,
                ApplicationFormat = ApplicationFormat.Consolidate,
                TraceMessages = true
            };
            var configDao = A.Fake<IOptionsMonitor<LogProcessingOptions>>();
            A.CallTo(() => configDao.CurrentValue).Returns(config);

            var converter = new Log4jConversionService(configDao, A.Fake<ILogger<Log4jConversionService>>());

            var logger = logListener.Setup<LogRepository>();

            return new LogRepository(socket, converter, configDao, logger);
        }
    }
}