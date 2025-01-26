// Copyright (C) 2024 Claudia Wagner

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Channel {

    public static class AsyncEnumerableExtensions {

        public static async IAsyncEnumerable<IList<TSource>> BatchAsync<TSource>(
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

            IAsyncEnumerator<TSource> enumerator = source.GetAsyncEnumerator(ct);
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

                while (!ct.IsCancellationRequested) {
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
                await FinalizeAsync(enumerator, timer).ConfigureAwait(false);
            }
        }

        private static async Task FinalizeAsync<TSource>(
            IAsyncEnumerator<TSource> enumerator,
            PeriodicTimer? timer) {
            try {
                timer?.Dispose();
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex) {
                Trace.WriteLine(ex);
            }
        }
    }
}
