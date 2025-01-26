// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Channel;
using Loginator.Domain.Model;
using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Channel {

    internal class LogWriter(
        ChannelWriter<Log> writer,
        Connection connection,
        ILogger<LogService> logger) : ILogWriter {

        private readonly ChannelWriter<Log> writer = writer;
        private readonly Connection connection = connection;
        private ILogRepository? logRepository;
        private CancellationTokenSource? cancellationTokenSource;
        private readonly ILogger<LogService> logger = logger;

        public Connection Connection =>
            connection;

        public Task Task { get; private set; } = Task.CompletedTask;

        public CancellationToken Token =>
            cancellationTokenSource is null ? CancellationToken.None : cancellationTokenSource.Token;

        public bool IsActive {
            get {
                return logRepository is not null && logRepository.IsActive;
            }
            set {
                if (logRepository is not null) logRepository.IsActive = value;
            }
        }

        public async ValueTask DisposeAsync() {
            if (cancellationTokenSource is not null) {
                await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                cancellationTokenSource = null;

                logRepository?.Dispose();
                logRepository = null;

                logger.LogInformation("Writer {connection} stopped", connection);
            }
            GC.SuppressFinalize(this);
        }

        public void Start(ILogRepositoryFactory logRepositoryFactory, CancellationToken ct) {
            if (Task.IsCompleted) {
                logRepository = logRepositoryFactory.CreateLogRepository(connection.ConnectionType, connection.LogType);

                Task = Task.Run(async () => {
                    try {
                        await WriteAsync(ct).ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException ex) {
                        logger.LogWarning(ex, "Repository {connection} closed unexpectedly, restarting receiver", connection);
                    }
                    catch (Exception ex) when (ex is OperationCanceledException || ex is SocketException) {
                        logger.LogWarning("Repository {connection} closed: {message}", connection, ex.Message);
                    }
                    catch (Exception ex) {
                        logger.LogError(ex, "Repository {connection} closed unexpectedly", connection);
                    }
                }, Token).ContinueWith(async _ => await DisposeAsync().ConfigureAwait(false));
            }
            else {
                logger.LogInformation("Writer {connection} is already running, no start required.", connection);
            }
        }

        public void Stop(CancellationToken ct) {
            if (Task.IsCompleted) {
                logger.LogInformation("Writer {connection} has already completed, no stop required.", connection);
            }
            else {
                CloseAsync().Wait(ct);
                Task.Wait(ct);
            }
        }

        public async Task CloseAsync() {
            await DisposeAsync().ConfigureAwait(false);
        }

        private async Task WriteAsync(CancellationToken ct) {
            logger.LogInformation("Writer {connection} starting", connection);

            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);

            await foreach (var item in logRepository!
                .GetEnumerableAsync(connection.Port, connection.IpAddress, Token)
                .ConfigureAwait(false)) {
                await writer.WriteAsync(item, Token).ConfigureAwait(false);
            }

            logger.LogInformation("Writer {connection} done", connection);
        }
    }
}
