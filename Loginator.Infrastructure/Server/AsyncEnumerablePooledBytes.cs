// Copyright (C) 2024 Claudia Wagner

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Server {

    internal class AsyncEnumerablePooledBytes<T>(
        AbstractSocket socket,
        ILogger<T> logger) : IAsyncEnumerable<PooledBytes> {

        private readonly AbstractSocket socket = socket;
        private readonly ILogger<T> logger = logger;

        public IAsyncEnumerator<PooledBytes> GetAsyncEnumerator(CancellationToken ct = default) =>
            new AsyncEnumerator(this, ct);

        public bool IsActive { get; set; } = true;

        private class AsyncEnumerator : IAsyncEnumerator<PooledBytes> {

            private const int BUFFER_LENGTH = 0x10000;

            private readonly CancellationToken cancelToken;
            private readonly AsyncEnumerablePooledBytes<T> outer;

            private readonly byte[] buffer;
            private readonly Memory<byte> bufferMemory;

            private AbstractSocket? connection;
            private PooledBytes? current;

            public AsyncEnumerator(AsyncEnumerablePooledBytes<T> outer, CancellationToken ct) {
                this.outer = outer;
                this.cancelToken = ct;

                // pre-pinned memory, using the .NET5 POH (pinned object heap)
                buffer = GC.AllocateArray<byte>(length: BUFFER_LENGTH, pinned: true);
                bufferMemory = buffer.AsMemory();
            }

            public async ValueTask<bool> MoveNextAsync() {
                current = await ReadBytesAsync().ConfigureAwait(false);
                return current is not null;
            }

            public PooledBytes Current => current!;

            public ValueTask DisposeAsync() {
                if (connection is not null && !ReferenceEquals(connection, outer.socket)) {
                    connection.Dispose();
                }
                return new ValueTask(Task.CompletedTask);
            }

            private async Task<PooledBytes?> ReadBytesAsync() {
                while (!cancelToken.IsCancellationRequested &&
                       await IsConnected().ConfigureAwait(false)) {
                    try {
                        var receivedBytes = connection is null ? 0 : await connection
                            .ReceiveAsync(bufferMemory, SocketFlags.None, cancelToken)
                            .ConfigureAwait(false);

                        if (receivedBytes > 0 && outer.IsActive) {
                            var pooledBytes = PooledBytes.Rent(receivedBytes);
                            Array.Copy(buffer, pooledBytes, receivedBytes);
                            return pooledBytes;
                        }
                    }
                    catch (SocketException ex) {
                        outer.logger.LogWarning("Connection access error: {message}", ex.Message);
                        connection?.Dispose();
                        connection = null;
                    }
                    catch (OperationCanceledException ex) {
                        outer.logger.LogInformation("Connection is closing: {message}", ex.Message);
                    }
                    catch (ObjectDisposedException ex) {
                        outer.logger.LogInformation("Connection is closing: {message}", ex.Message);
                        break;
                    }
                }
                return null;
            }

            private async Task<bool> IsConnected() {
                connection ??= await outer.socket.AcceptAsync(cancelToken).ConfigureAwait(false);
                return await outer.socket.IsConnectedAsync(connection, cancelToken).ConfigureAwait(false);
            }
        }
    }
}
