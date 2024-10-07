// Copyright (C) 2024 Claudia Wagner

using Backend.Converter;
using Backend.Model;
using Backend.Server;
using Common;
using Common.Configuration;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Backend.UnitTests.Server.ReceiverTestData;

namespace Backend.UnitTests.Server {

    /// <summary>
    /// Represents unit tests for <see cref="Receiver"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ReceiverTests {

        private const string EMPTY_LOG = "<event></event>";
        private const string NO_LOG = "no log";
        private static readonly TimeSpan CancelTimespan = TimeSpan.FromMilliseconds(100);

        private readonly AbstractSocket socket;
        private readonly SocketServer socketServer;
        private readonly Receiver sut;

        public ReceiverTests() {
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

            await foreach (var actual in sut.ReadAsync(0, socketServer.CancellationToken)) {
                actual.Should().Be(ValidLog, new LogComparer());
            }
        }

        [Test]
        public async Task Can_listen_for_client_activity_on_all_network_interfaces() {
            var expectedPort = 1234;
            var expectedEndpoint = new IPEndPoint(IPAddress.Any, expectedPort);
            socketServer.SetReturnValues(EMPTY_LOG, NO_LOG, EMPTY_LOG);

            await foreach (var _ in sut.ReadAsync(expectedPort, socketServer.CancellationToken)) {
            }

            A.CallTo(() => socket.Bind(An<EndPoint>.That.IsEqualTo(expectedEndpoint)))
                .MustHaveHappenedOnceExactly();
        }

        private async Task AssertCancellation(int expectedCallCount) {
            var actualCallCount = 0;

            await foreach (var _ in sut.ReadAsync(0, socketServer.CancellationToken)) {
                if (++actualCallCount == expectedCallCount) socketServer.Cancel();
            }

            actualCallCount.Should().Be(expectedCallCount);
        }

        private Receiver Sut() {
            A.CallTo(() => socket.Accept())
                .Returns(socket);
            A.CallTo(() => socket.IsConnected(A<Socket>._, A<CancellationToken>._))
                .Returns(true);
            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .ReturnsLazily(socketServer.FillMemoryAndReturnLength).NumberOfTimes(1000);

            var config = new Configuration {
                AllowAnonymousLogs = true,
                ApplicationFormat = ApplicationFormat.Consolidate
            };
            var configDao = A.Fake<IOptionsMonitor<Configuration>>();
            A.CallTo(() => configDao.CurrentValue).Returns(config);

            var appConfig = new ApplicationConfiguration {
                IsMessageTraceEnabled = true
            };
            var appConfigDao = A.Fake<IOptionsMonitor<ApplicationConfiguration>>();
            A.CallTo(() => appConfigDao.CurrentValue).Returns(appConfig);

            var converter = new ChainsawToLogConverter(configDao, A.Fake<ILogger<ChainsawToLogConverter>>());
            return new Receiver(socket, converter, appConfigDao, A.Fake<ILogger<Receiver>>());
        }
    }
}