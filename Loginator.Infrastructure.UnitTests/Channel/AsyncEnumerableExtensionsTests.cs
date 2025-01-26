// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Loginator.Infrastructure.Channel.AsyncEnumerableExtensions;

namespace Loginator.Infrastructure.UnitTests.Channel {

    /// <summary>
    /// Represents unit tests for <see cref="AsyncEnumerableExtensions"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase), Parallelizable(ParallelScope.All)]
    public class AsyncEnumerableExtensionsTests {

        private static readonly TimeSpan DEFAULT_INTERVAL = TimeSpan.FromSeconds(1);

        private readonly FakeTimeProvider timeProvider = new();

        [CancelAfter(2000)]
        [TestCase(100, 1000, 2200)]
        [TestCase(100, 10, 2000)]
        public async Task Can_batch_asynchronously_enumerable_items(int expectedCount, long readMillis, long batchMillis) {
            var readInterval = TimeSpan.FromMilliseconds(readMillis);
            var testItems = SetupItems(Enumerable.Range(1, expectedCount), readInterval);
            var batchInterval = TimeSpan.FromMilliseconds(batchMillis);
            var cts = new CancellationTokenSource();

            var read = ReadtemsAsync(testItems, expectedCount, batchInterval, cts);
            await InjectItemsAsync(readInterval, cts.Token).ConfigureAwait(false);
            await read.ConfigureAwait(false);
        }

        [TestCase("test", null, 1)]
        [TestCase("test1", "test2", 2)]
        public async Task Can_batch_enumerable_items(string item1, string? item2, int expectedCount) {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async IAsyncEnumerable<string> EnumerableAsync() {
                yield return item1;
                if (item2 is not null) yield return item2;
            }
#pragma warning restore CS1998

            await foreach (var items in EnumerableAsync()
                .BatchAsync(DEFAULT_INTERVAL, timeProvider, default)) {
                items.Should().HaveCount(expectedCount);
            }
        }

        private async Task InjectItemsAsync(TimeSpan interval, CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                timeProvider.Advance(interval);
                await Task.Yield();
            }
        }

        private async Task ReadtemsAsync(AsyncEnumerableClocked<int> testItems, int expectedCount, TimeSpan batchInterval, CancellationTokenSource cts) {
            var actualCount = 0;

            await foreach (var items in testItems.GetEnumerableAsync(cts.Token)
                .BatchAsync(batchInterval, timeProvider, cts.Token).ConfigureAwait(false)) {
                var atLeastExpected = actualCount < expectedCount - 1 ? 2 : 1;
                items.Should().HaveCountGreaterThanOrEqualTo(atLeastExpected);

                actualCount += items.Count;
                TestContext.Out.WriteLine($"{timeProvider.GetUtcNow()}: received {items.Count}, so far {actualCount}");

                if (actualCount == expectedCount) {
                    cts.Cancel();
                    await Task.Yield();
                    break;
                }
            }
        }

        private AsyncEnumerableClocked<T> SetupItems<T>(IEnumerable<T> items, TimeSpan? timeSpan = null) {
            var queue = new AsyncEnumerableClocked<T>(timeSpan ?? DEFAULT_INTERVAL, timeProvider);

            foreach (var item in items.Reverse()) {
                // items are added in front
                queue.Enqueue(item);
            }
            return queue;
        }
    }
}