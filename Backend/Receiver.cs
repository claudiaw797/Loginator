// Copyright (C) 2024 Claudia Wagner

using Backend.Converter;
using Backend.Model;
using Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Backend {

    public sealed class Receiver : IReceiver {

        private const int BUFFER_LENGTH = 0x10000;

        private readonly ILogConverter converter;
        private readonly IOptionsMonitor<ApplicationConfiguration> applicationConfiguration;
        private readonly ILogger<Receiver> logger;

        public Receiver(ILogConverter converter, IOptionsMonitor<ApplicationConfiguration> applicationConfiguration, ILogger<Receiver> logger) {
            this.converter = converter;
            this.applicationConfiguration = applicationConfiguration;
            this.logger = logger;
        }

        public async IAsyncEnumerable<Log> ReadAsync(int port, [EnumeratorCancellation] CancellationToken ct) {
            using var udpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using var cancelReg = ct.Register(() => udpSocket?.Close());

            udpSocket.Bind(new IPEndPoint(IPAddress.Any, port));

            await foreach (var pooledBytes in ReceiveAsync(udpSocket, ct)) {
                LogReceivedText(pooledBytes);

                foreach (var log in converter.Convert(pooledBytes).Where(l => l != Log.DEFAULT)) {
                    yield return log;
                }
                pooledBytes.Dispose();
            };
        }

        private void LogReceivedText(Stream stream) {
            if (applicationConfiguration.CurrentValue.IsMessageTraceEnabled) {
                Task.Run(() => {
                    var receivedText = new StreamReader(stream).ReadToEnd();
                    logger.LogTrace(receivedText);
                });
            }
        }

        private async IAsyncEnumerable<PooledBytes> ReceiveAsync(Socket udpSocket, [EnumeratorCancellation] CancellationToken ct) {
            // taking advantage of pre-pinned memory, using the .NET5 POH (pinned object heap)
            var buffer = GC.AllocateArray<byte>(length: BUFFER_LENGTH, pinned: true);
            var bufferMem = buffer.AsMemory();
            var receivedAddress = new SocketAddress(udpSocket.AddressFamily);
            int received;

            while (!ct.IsCancellationRequested) {
                try {
                    received = await udpSocket
                        .ReceiveFromAsync(bufferMem, SocketFlags.None, receivedAddress, ct)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) {
                    logger.LogInformation("Receiver stopped: {0}", ex.Message);
                    break;
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Could not read package");
                    received = 0;
                }

                if (received > 0) {
                    var pooledBytes = PooledBytes.Rent(received);
                    Array.Copy(buffer, pooledBytes, received);
                    yield return pooledBytes;
                }
            }
        }
    }
}
