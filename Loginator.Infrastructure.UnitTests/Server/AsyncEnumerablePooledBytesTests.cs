// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Infrastructure.Server;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.UnitTests.Server {

    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class AsyncEnumerablePooledBytesTests {

        private static readonly TimeSpan CancelTimespan = TimeSpan.FromMilliseconds(100);

        private readonly AbstractSocket socket;
        private readonly FakeSocketServer socketServer = new();
        private readonly LogListener logListener = new();
        private readonly AsyncEnumerablePooledBytes<LogRepository> sut;

        public AsyncEnumerablePooledBytesTests() {
            socket = A.Fake<AbstractSocket>();
            sut = Sut();
        }

        [Test]
        public async Task Can_receive_bytes() {
            var value = "test value";
            socketServer.SetReturnValues(value);
            socketServer.AutoCancel = false;

            await TestReceptionAsync(value);

            logListener.LogCalls.Should().BeEmpty();
        }

        [Test]
        public async Task Can_receive_bytes_after_socket_access_error_occurred() {
            var value = "test value";
            socketServer.SetReturnValues(value);
            socketServer.AutoCancel = false;

            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .Throws(new SocketException(123, value)).Once();

            await TestReceptionAsync(value);

            logListener.Contains(LogLevel.Warning, messagePattern: value).Should().BeTrue();
        }

        [Test]
        public async Task Can_finish_when_socket_closed() {
            var value = "test error";

            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .Throws(new ObjectDisposedException(value)).Once();

            await TestNonReceptionAsync();

            logListener.Contains(LogLevel.Information, messagePattern: value).Should().BeTrue();
        }

        [Test]
        public async Task Can_receive_bytes_from_renewed_connection() {
            var value = "test value";
            socketServer.SetReturnValues(value);
            socketServer.AutoCancel = false;

            A.CallTo(() => socket.AcceptAsync(A<CancellationToken>._))
                .Returns(null!).Once();

            await TestReceptionAsync(value);

            logListener.LogCalls.Should().BeEmpty();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Can_discard_received_bytes_when_deactivated(bool once) {
            var value = "test value";
            socketServer.SetReturnValues(value);
            socketServer.AutoCancel = once;
            if (!once) {
                socketServer.Cancel(CancelTimespan);
            }
            sut.IsActive = false;

            await TestNonReceptionAsync();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Can_discard_received_zero_bytes(bool once) {
            socketServer.SetReturnValues();
            socketServer.AutoCancel = once;
            if (!once) {
                socketServer.Cancel(CancelTimespan);
            }

            await TestNonReceptionAsync();
        }

        [Test]
        public async Task Can_finish_when_token_is_canceled() {
            var value = "test error";

            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .Throws(new OperationCanceledException(value));

            socketServer.Cancel(CancelTimespan);

            await TestNonReceptionAsync();

            logListener.Contains(LogLevel.Information, messagePattern: value, expectedTimes: logListener.LogCalls.Count).Should().BeTrue();
        }

        [Test]
        public async Task Can_dispose_only_accepted_socket() {
            var value = "test error";
            socketServer.SetReturnValues(value);
            socketServer.AutoCancel = false;

            await TestDisposeWithoutConnectionAsync();
            await TestDisposeWithConnectionAsync();
            await TestDisposeWithAcceptedSocketAsync();

            logListener.LogCalls.Should().BeEmpty();
        }

        private async Task TestReceptionAsync(string value) {
            var innerSut = sut.GetAsyncEnumerator(socketServer.CancellationToken);

            var actualNext = await innerSut.MoveNextAsync().ConfigureAwait(true);
            using var actualBytes = innerSut.Current;

            actualNext.Should().BeTrue();
            var result = Encoding.UTF8.GetString(actualBytes.ToArray());
            result.Should().Be(value);
        }

        private async Task TestNonReceptionAsync() {
            var innerSut = sut.GetAsyncEnumerator(socketServer.CancellationToken);

            var actualNext = await innerSut.MoveNextAsync().ConfigureAwait(true);
            using var actualBytes = innerSut.Current;

            actualNext.Should().BeFalse();
            actualBytes.Should().BeNull();
        }

        private async Task TestDisposeWithoutConnectionAsync() {
            var innerSut = sut.GetAsyncEnumerator(socketServer.CancellationToken);

            // connection is null
            await innerSut.DisposeAsync();

            A.CallTo(() => socket.Dispose()).MustNotHaveHappened();
        }

        private async Task TestDisposeWithConnectionAsync() {
            var innerSut = sut.GetAsyncEnumerator(socketServer.CancellationToken);

            // connection is not accepted socket
            await innerSut.MoveNextAsync().ConfigureAwait(true);
            await innerSut.DisposeAsync();

            A.CallTo(() => socket.Dispose()).MustNotHaveHappened();
        }

        private async Task TestDisposeWithAcceptedSocketAsync() {
            var innerSut = sut.GetAsyncEnumerator(socketServer.CancellationToken);

            var acceptedSocket = A.Fake<AbstractSocket>();
            A.CallTo(() => socket.AcceptAsync(A<CancellationToken>._))
                .Returns(acceptedSocket);
            A.CallTo(() => socket.IsConnectedAsync(A<Socket>._, A<CancellationToken>._))
                .Returns(false);

            // connection is accepted socket
            await innerSut.MoveNextAsync().ConfigureAwait(true);
            await innerSut.DisposeAsync();

            A.CallTo(() => socket.Dispose()).MustNotHaveHappened();
            A.CallTo(() => acceptedSocket.Dispose()).MustHaveHappened();
        }

        private AsyncEnumerablePooledBytes<LogRepository> Sut() {
            A.CallTo(() => socket.AcceptAsync(A<CancellationToken>._))
                .Returns(socket);
            A.CallTo(() => socket.IsConnectedAsync(A<Socket>._, A<CancellationToken>._))
                .Returns(true);
            A.CallTo(() => socket.ReceiveAsync(A<Memory<byte>>._, A<SocketFlags>._, A<CancellationToken>._))
                .ReturnsLazily(socketServer.FillMemoryAndReturnLength);

            var logger = logListener.Setup<LogRepository>();
            return new AsyncEnumerablePooledBytes<LogRepository>(socket, logger);
        }
    }
}