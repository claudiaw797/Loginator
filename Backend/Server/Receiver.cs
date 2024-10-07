// Copyright (C) 2024 Claudia Wagner

using Backend.Converter;
using Backend.Model;
using Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.Server {

    public sealed class Receiver : IReceiver {

        private readonly AbstractSocket socket;
        private readonly ILogConverter converter;
        private readonly IOptionsMonitor<ApplicationConfiguration> applicationConfiguration;
        private readonly ILogger<Receiver> logger;

        internal Receiver(AbstractSocket socket, ILogConverter converter, IOptionsMonitor<ApplicationConfiguration> applicationConfiguration, ILogger<Receiver> logger) {
            this.socket = socket;
            this.converter = converter;
            this.applicationConfiguration = applicationConfiguration;
            this.logger = logger;
        }

        public async IAsyncEnumerable<Log> ReadAsync(int port, [EnumeratorCancellation] CancellationToken cancelToken) {
            using var cancelReg = cancelToken.Register(() => socket.Close());

            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();

            await foreach (var pooledBytes in new AsyncEnumerablePooledBytes<Receiver>(socket, logger)
                .WithCancellation(cancelToken)) {
                try {
                    LogReceivedText(pooledBytes);

                    foreach (var log in converter.Convert(pooledBytes).Where(l => l != Log.DEFAULT)) {
                        yield return log;
                    }
                }
                finally {
                    pooledBytes.Dispose();
                }
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
    }
}
