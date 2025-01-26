// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Channel;
using Loginator.Domain.Model;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.UnitTests.Channel {

    internal class LogAsyncEnumerableQueueReader : ILogProcessor {

        private static readonly TimeSpan TIMESPAN = TimeSpan.FromSeconds(1);

        private readonly AsyncEnumerableQueue<Log> incomingLogs = new();
        private readonly List<Log> receivedLogs = [];

        public LogAsyncEnumerableQueueReader(ChannelReader<Log>? reader = null) {
            if (reader is not null) _ = ReadAsync(reader);
        }

        public FakeTimeProvider TimeProvider { get; init; } = new();

        public IAsyncEnumerable<Log> IncomingLogs => incomingLogs;

        public IReadOnlyCollection<Log> ReceivedLogs => receivedLogs;

        public async Task AddItemsAsync(IEnumerable<Log> items, bool isComplete = true) {
            var itemCount = items.Count();
            foreach (var item in items) {
                incomingLogs.Enqueue(item);
            }

            while (true) {
                TimeProvider.Advance(TIMESPAN);
                await Task.Yield();

                if (receivedLogs.Count == itemCount) {
                    incomingLogs.IsCompleted = isComplete;
                    await Task.Yield();
                    break;
                }
            }
        }

        public void Complete() =>
            incomingLogs.IsCompleted = true;

        void ILogProcessor.ProcessLogs(IEnumerable<Log> logs) {
            receivedLogs.AddRange(logs);
        }

        private async Task ReadAsync(ChannelReader<Log> reader) {
            await foreach (var log in reader.ReadAllAsync().ConfigureAwait(false)) {
                receivedLogs.Add(log);
            }
        }
    }
}
