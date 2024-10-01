// Copyright (C) 2024 Claudia Wagner

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.UnitTests {

    internal class AsyncEnumerableQueue<T> : ConcurrentQueue<T>, IAsyncEnumerable<T> {

        public bool IsCompleted { get; set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new AsyncEnumerator(this);

        private class AsyncEnumerator(AsyncEnumerableQueue<T> inner) : IAsyncEnumerator<T> {

            private readonly AsyncEnumerableQueue<T> inner = inner;

            public ValueTask<bool> MoveNextAsync() {
                var tcs = new TaskCompletionSource<bool>();
                Task.Run(async () => {
                    while (true) {
                        if (inner.IsCompleted) {
                            tcs.SetResult(false);
                            break;
                        }
                        if (inner.TryPeek(out var _)) {
                            tcs.SetResult(true);
                            break;
                        }
                        await Task.Yield();
                    }
                });
                return new(tcs.Task);
            }

            public T Current => inner.TryDequeue(out var t) ? t : default!;

            public ValueTask DisposeAsync() {
                inner.IsCompleted = true;

                return new ValueTask(Task.CompletedTask);
            }
        }
    }
}
