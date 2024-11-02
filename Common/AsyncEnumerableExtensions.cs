// Copyright (C) 2024 Claudia Wagner

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Application.Common {

    internal static class AsyncEnumerableExtensions {

        public static async IAsyncEnumerable<IList<TSource>> Batch<TSource>(
            this IAsyncEnumerable<TSource> source,
            TimeSpan timeSpan,
            TimeProvider timeProvider,
            [EnumeratorCancellation] CancellationToken ct = default) {
            ArgumentNullException.ThrowIfNull(source);

            PeriodicTimer? timer = null;
            Task<bool> StartTimer() {
                timer = new(timeSpan, timeProvider);
                return timer.WaitForNextTickAsync(ct).AsTask();
            }

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            IAsyncEnumerator<TSource> enumerator = source.GetAsyncEnumerator(linkedCts.Token);
            Task<bool>? moveNext = null;
            try {
                List<TSource> buffer = [];
                Task<bool> timerTickTask;
                TSource[] ConsumeBuffer() {
                    timer?.Dispose();
                    TSource[] array = [.. buffer];
                    buffer.Clear();
                    return array;
                }

                while (true) {
                    moveNext ??= enumerator.MoveNextAsync().AsTask();

                    var hasNext = await moveNext.ConfigureAwait(false);
                    if (!hasNext) break;

                    buffer.Add(enumerator.Current);

                    moveNext = enumerator.MoveNextAsync().AsTask();
                    timerTickTask = StartTimer();

                    var completedTask = await Task.WhenAny(moveNext, timerTickTask).ConfigureAwait(false);
                    if (ReferenceEquals(completedTask, timerTickTask))
                        yield return ConsumeBuffer();
                    else
                        timer?.Dispose();
                }

                if (buffer.Count > 0) yield return ConsumeBuffer();
            }
            finally {
                try {
                    // cancel enumerator for more responsive completion
                    linkedCts.Cancel();
                }
                finally {
                    // last moveNext must be completed before disposing
                    if (moveNext is not null && !moveNext.IsCompleted)
                        await moveNext.ConfigureAwait(false);
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                    timer?.Dispose();
                }
            }
        }
    }
}
