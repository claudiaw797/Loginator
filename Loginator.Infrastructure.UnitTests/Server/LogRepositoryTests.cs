// Copyright (C) 2024 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Domain.Option;
using Loginator.Infrastructure.Converter;
using Loginator.Infrastructure.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Loginator.Infrastructure.UnitTests.Server.LogRepositoryTestData;

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
        private readonly SocketServer socketServer;
        private readonly LogRepository sut;

        public LogRepositoryTests() {
            socket = A.Fake<AbstractSocket>();
            socketServer = new();
            sut = Sut();
        }

        [Test]
        public async Task Can_cancel_operation_after_reception_of_valid_logs() {
            var expectedCallCount = 5;
            socketServer.SetReturnValues(EMPTY_LOG);
            socketServer.AutoCancel = false;

            await AssertCancellation(expectedCallCount);
        }

        [Test]
        public async Task Can_cancel_operation_after_reception_of_invalid_logs() {
            var expectedCallCount = 0;
            socketServer.SetReturnValues(NO_LOG);
            socketServer.AutoCancel = false;
            socketServer.Cancel(CancelTimespan);

            await AssertCancellation(expectedCallCount);
        }

        [Test]
        public async Task Can_cancel_operation_after_reception_of_zero_bytes() {
            var expectedCallCount = 0;
            socketServer.SetReturnValues();
            socketServer.AutoCancel = false;
            socketServer.Cancel(CancelTimespan);

            await AssertCancellation(expectedCallCount);
        }

        [Test]
        public async Task Can_cancel_operation_after_waiting_without_reception() {
            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .ReturnsLazily(socketServer.WaitUntilCanceled);

            var expectedCallCount = 0;
            socketServer.Cancel(CancelTimespan);

            await AssertCancellation(expectedCallCount);
        }

        [Test]
        public async Task Can_convert_valid_log4j_strings_to_logs() {
            socketServer.SetReturnValues(ValidLogMessages().ToArray());

            await foreach (var actual in sut.GetEnumerableAsync(0, socketServer.CancellationToken)) {
                actual.Should().Be(ValidLog, new LogComparer());
            }
        }

        [Test]
        public async Task Can_listen_for_client_activity_on_all_network_interfaces() {
            var expectedPort = 1234;
            var expectedEndpoint = new IPEndPoint(IPAddress.Any, expectedPort);
            socketServer.SetReturnValues(EMPTY_LOG, NO_LOG, EMPTY_LOG);

            await foreach (var _ in sut.GetEnumerableAsync(expectedPort, socketServer.CancellationToken)) {
            }

            A.CallTo(() => socket.Bind(An<EndPoint>.That.IsEqualTo(expectedEndpoint)))
                .MustHaveHappenedOnceExactly();
        }

        private async Task AssertCancellation(int expectedCallCount) {
            var actualCallCount = 0;

            await foreach (var _ in sut.GetEnumerableAsync(0, socketServer.CancellationToken)) {
                if (++actualCallCount == expectedCallCount) socketServer.Cancel();
            }

            actualCallCount.Should().Be(expectedCallCount);
        }

        private LogRepository Sut() {
            A.CallTo(() => socket.Accept())
                .Returns(socket);
            A.CallTo(() => socket.IsConnected(A<Socket>._, A<CancellationToken>._))
                .Returns(true);
            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .ReturnsLazily(socketServer.FillMemoryAndReturnLength).NumberOfTimes(1000);

            var config = new LogProcessingOptions {
                AllowAnonymousMessages = true,
                ApplicationFormat = ApplicationFormat.Consolidate,
                TraceMessages = true
            };
            var configDao = A.Fake<IOptionsMonitor<LogProcessingOptions>>();
            A.CallTo(() => configDao.CurrentValue).Returns(config);

            var converter = new Log4jConversionFactory(configDao, A.Fake<ILogger<Log4jConversionFactory>>());
            return new LogRepository(socket, converter, configDao, A.Fake<ILogger<LogRepository>>());
        }
    }
}