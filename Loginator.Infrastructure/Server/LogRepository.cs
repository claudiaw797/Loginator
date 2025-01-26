// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Converter;
using Loginator.Domain.Model;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Server {

    internal sealed class LogRepository : ILogRepository, IDisposable {

        private readonly AbstractSocket socket;
        private readonly AsyncEnumerablePooledBytes<LogRepository> enumerableBytes;
        private readonly ILogConversionService conversionFactory;
        private readonly IOptionsMonitor<LogProcessingOptions> optionsMonitor;
        private readonly ILogger<LogRepository> logger;

        internal LogRepository(
            AbstractSocket socket,
            ILogConversionService conversionFactory,
            IOptionsMonitor<LogProcessingOptions> optionsMonitor,
            ILogger<LogRepository> logger) {
            this.socket = socket;
            this.enumerableBytes = new(socket, logger);
            this.conversionFactory = conversionFactory;
            this.optionsMonitor = optionsMonitor;
            this.logger = logger;
        }

        public bool IsActive {
            get => enumerableBytes.IsActive;
            set => enumerableBytes.IsActive = value;
        }

        public void Dispose() {
            socket.Close();
        }

        public async IAsyncEnumerable<Log> GetEnumerableAsync(int port, string? ipAddress, [EnumeratorCancellation] CancellationToken ct) {
            if (!socket.IsBound) {
                var address = string.IsNullOrWhiteSpace(ipAddress)
                    ? IPAddress.Any
                    : IPAddress.Parse(ipAddress);
                socket.Bind(new IPEndPoint(address, port));
                socket.Listen();
            }

            await foreach (var pooledBytes in enumerableBytes
                .WithCancellation(ct)
                .ConfigureAwait(false)) {
                try {
                    TraceMessage(pooledBytes);

                    foreach (var log in conversionFactory.Convert(pooledBytes).Where(l => l != Log.DEFAULT)) {
                        yield return log;
                    }
                }
                finally {
                    pooledBytes.Dispose();
                }
            };
        }

        private void TraceMessage(Stream stream) {
            if (optionsMonitor.CurrentValue.TraceMessages) {
                Task.Run(() => {
                    var receivedText = new StreamReader(stream).ReadToEnd();
                    logger.LogTrace("{text}", receivedText);
                });
            }
        }
    }
}
