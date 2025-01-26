// Copyright (C) 2024 Claudia Wagner

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.UnitTests.Infrastructure {

    public class AsyncEnumerableQueue<T> : ConcurrentQueue<T>, IAsyncEnumerable<T> {

        public bool IsCompleted { get; set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default) =>
            new AsyncEnumerator(this, ct);

        private class AsyncEnumerator(AsyncEnumerableQueue<T> outer, CancellationToken ct) : IAsyncEnumerator<T> {

            public ValueTask<bool> MoveNextAsync() {
                var tcs = new TaskCompletionSource<bool>();
                Task.Run(async () => {
                    while (true) {
                        if (outer.IsCompleted) {
                            tcs.SetResult(false);
                            break;
                        }
                        if (outer.TryPeek(out var _)) {
                            tcs.SetResult(true);
                            break;
                        }
                        await Task.Yield();
                    }
                }, ct);
                return new(tcs.Task);
            }

            public T Current {
                get {
                    outer.TryDequeue(out var t);
                    return t!;
                }
            }

            public ValueTask DisposeAsync() {
                outer.IsCompleted = true;

                return new ValueTask(Task.CompletedTask);
            }
        }
    }
}
