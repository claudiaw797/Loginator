// Copyright (C) 2024 Claudia Wagner

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.Server {

    internal class AsyncEnumerablePooledBytes<T>(AbstractSocket socket, ILogger<T> logger) : IAsyncEnumerable<PooledBytes> {

        private readonly AbstractSocket socket = socket;
        private readonly ILogger<T> logger = logger;

        public IAsyncEnumerator<PooledBytes> GetAsyncEnumerator(CancellationToken cancelToken = default) =>
            new AsyncEnumerator(this, cancelToken);

        private class AsyncEnumerator : IAsyncEnumerator<PooledBytes> {

            private const int BUFFER_LENGTH = 0x10000;

            private readonly CancellationToken cancelToken;
            private readonly AsyncEnumerablePooledBytes<T> outer;

            private readonly byte[] buffer;
            private readonly Memory<byte> bufferMemory;

            private AbstractSocket? connection;
            private PooledBytes? current;

            public AsyncEnumerator(AsyncEnumerablePooledBytes<T> outer, CancellationToken cancelToken) {
                this.outer = outer;
                this.cancelToken = cancelToken;

                // pre-pinned memory, using the .NET5 POH (pinned object heap)
                buffer = GC.AllocateArray<byte>(length: BUFFER_LENGTH, pinned: true);
                bufferMemory = buffer.AsMemory();
            }

            public async ValueTask<bool> MoveNextAsync() {
                current = await ReadBytesAsync();
                return current is not null;
            }

            public PooledBytes Current => current!;

            public ValueTask DisposeAsync() {
                connection?.Dispose();
                outer.socket.Dispose();
                return new ValueTask(Task.CompletedTask);
            }

            private async Task<PooledBytes?> ReadBytesAsync() {
                while (!cancelToken.IsCancellationRequested &&
                       await IsConnected()) {
                    try {
                        var receivedBytes = connection is null ? 0 : await connection
                            .ReceiveAsync(bufferMemory, SocketFlags.None, cancelToken)
                            .ConfigureAwait(false);

                        if (receivedBytes > 0) {
                            var pooledBytes = PooledBytes.Rent(receivedBytes);
                            Array.Copy(buffer, pooledBytes, receivedBytes);
                            return pooledBytes;
                        }
                    }
                    catch (SocketException ex) {
                        outer.logger.LogWarning("Connection was lost: {message}", ex.Message);
                        connection?.Dispose();
                        connection = null;
                    }
                    catch (OperationCanceledException ex) {
                        outer.logger.LogInformation("Connection is closing: {message}", ex.Message);
                        break;
                    }
                    catch (Exception ex) {
                        outer.logger.LogError(ex, "Could not read package");
                    }
                }
                return null;
            }

            private async Task<bool> IsConnected() {
                connection ??= outer.socket.Accept();
                return await outer.socket.IsConnected(connection, cancelToken);
            }
        }
    }
}
