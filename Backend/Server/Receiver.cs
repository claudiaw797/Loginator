// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Converter;
using Loginator.Domain.Model;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Server {

    internal sealed class LogRepository : ILogRepository {

        private readonly AbstractSocket socket;
        private readonly ILogConversionFactory conversionFactory;
        private readonly IOptionsMonitor<LogProcessingOptions> optionsMonitor;
        private readonly ILogger<LogRepository> logger;

        internal LogRepository(AbstractSocket socket, ILogConversionFactory conversionFactory, IOptionsMonitor<LogProcessingOptions> optionsMonitor, ILogger<LogRepository> logger) {
            this.socket = socket;
            this.conversionFactory = conversionFactory;
            this.optionsMonitor = optionsMonitor;
            this.logger = logger;
        }

        public async IAsyncEnumerable<Log> GetEnumerableAsync(int port, [EnumeratorCancellation] CancellationToken cancelToken) {
            using var cancelReg = cancelToken.Register(() => socket.Close());

            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();

            await foreach (var pooledBytes in new AsyncEnumerablePooledBytes<LogRepository>(socket, logger)
                .WithCancellation(cancelToken)) {
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
                    logger.LogTrace(receivedText);
                });
            }
        }
    }
}
