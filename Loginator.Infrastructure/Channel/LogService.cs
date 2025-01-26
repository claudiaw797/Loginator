// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Channel;
using Loginator.Domain.Model;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Transactions;
using SysChannel = System.Threading.Channels.Channel;

namespace Loginator.Infrastructure.Channel {

    internal sealed class LogService : ILogService {

        private readonly ILogRepositoryFactory logRepositoryFactory;
        private readonly ILogger<LogService> logger;

        private readonly CancellationTokenSource cancellationTokenSource = new();

        private readonly Channel<Log> channel;
        private readonly List<ILogWriter> writers = [];

        public LogService(
            ILogProcessor processor,
            ILogRepositoryFactory logRepositoryFactory,
            TimeProvider timeProvider,
            ILogger<LogService> logger) {
            this.logRepositoryFactory = logRepositoryFactory;
            this.logger = logger;

            channel = SysChannel.CreateUnbounded<Log>(new() { SingleReader = true });

            _ = new LogReader(channel.Reader, timeProvider, logger).ReadAsync(processor.ProcessLogs, Token);
        }

        public CancellationToken Token =>
            cancellationTokenSource.Token;

        public ILogWriter CreateWriter(Connection connection) {
            var logWriter = new LogWriter(channel.Writer, connection, logger);
            writers.Add(logWriter);
            return logWriter;
        }

        public void StartWriter(ILogWriter logWriter) {
            logWriter.Start(logRepositoryFactory, Token);
        }

        public void StopWriter(ILogWriter logWriter) {
            logWriter.Stop(Token);
        }

        public void RemoveWriter(ILogWriter logWriter) {
            using TransactionScope scope = new TransactionScope();
            logWriter.Stop(Token);
            writers.Remove(logWriter);
            scope.Complete();
        }

        public async ValueTask DisposeAsync() {
            try {
                if (!cancellationTokenSource.IsCancellationRequested) {
                    await cancellationTokenSource.CancelAsync().ConfigureAwait(false);

                    logger.LogInformation("Channel writer completing...");
                }
                await Task.WhenAll(writers.Select(w => w.Task)).ConfigureAwait(false);
            }
            catch (Exception ex) {
                logger.LogError(ex, "Error trying to complete all Writers");
            }
            finally {
                channel.Writer.TryComplete();
                cancellationTokenSource.Dispose();
                logger.LogInformation("Channel writer completed");
            }
        }

        internal class LogReader(
            ChannelReader<Log> reader,
            TimeProvider timeProvider,
            ILogger<LogService> logger) {

            private static readonly TimeSpan BATCH_TIME_INTERVAL = TimeSpan.FromMilliseconds(300);

            private readonly TimeProvider timeProvider = timeProvider;
            private readonly ChannelReader<Log> reader = reader;
            private readonly ILogger<LogService> logger = logger;

            public async Task ReadAsync(Action<IEnumerable<Log>> processor, CancellationToken ct) {
                logger.LogInformation("Reader starting");

                await foreach (var item in reader
                    .ReadAllAsync(ct)
                    .BatchAsync(BATCH_TIME_INTERVAL, timeProvider, ct)) {
                    processor.Invoke(item);
                }

                logger.LogInformation("Reader done");
            }
        }
    }
}
