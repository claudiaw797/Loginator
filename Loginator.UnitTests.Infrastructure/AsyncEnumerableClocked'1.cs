// Copyright (C) 2025 Claudia Wagner

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.UnitTests.Infrastructure {

    public class AsyncEnumerableClocked<T>(TimeSpan timeSpan, TimeProvider timeProvider) : ConcurrentQueue<T> {

        public async IAsyncEnumerable<T> GetEnumerableAsync([EnumeratorCancellation] CancellationToken ct) {
            var timer = new PeriodicTimer(timeSpan, timeProvider);
            try {
                while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
                    if (this.TryDequeue(out var t)) {
                        TestContext.Out.WriteLine($"{timeProvider.GetUtcNow()}: returning {t}");
                        yield return t;
                    }
                    await Task.Yield();
                }
            }
            finally {
                timer.Dispose();
            }
        }
    }
}
