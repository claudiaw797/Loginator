// Copyright (C) 2025 Claudia Wagner

using FakeItEasy.Core;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.UnitTests.Infrastructure {

    public class FakeSocketServer {

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private string[] returnValues = [];
        private int callCounter = 0;

        public bool AutoCancel { get; set; } = true;

        public CancellationToken CancellationToken =>
            cancellationTokenSource.Token;

        public void Cancel(TimeSpan? delay = null) {
            if (delay.HasValue)
                cancellationTokenSource.CancelAfter(delay.Value);
            else
                cancellationTokenSource.Cancel();
        }

        public void SetReturnValues(params string[] returnValues) =>
            this.returnValues = returnValues;

        public ValueTask<int> FillMemoryAndReturnLength(IFakeObjectCall call) {
            if (CancellationToken.IsCancellationRequested) {
                return new(0);
            }

            if (returnValues is null || returnValues.Length == 0) {
                if (AutoCancel) Cancel();
                return new(0);
            }

            if (callCounter >= returnValues.Length) {
                if (AutoCancel) {
                    Cancel();
                    return new(0);
                }

                callCounter = 0;
            }

            TestContext.Out.WriteLine(returnValues[callCounter]);
            var memory = Encoding.UTF8.GetBytes(returnValues[callCounter++]).AsMemory();
            var argument = call.Arguments.Get<Memory<byte>>(0);
            memory.CopyTo(argument);
            return new(memory.Length);
        }

        public ValueTask<int> WaitUntilCanceled(IFakeObjectCall _) {
            var tcs = new TaskCompletionSource<int>();
            Task.Factory.StartNew(async () => {
                await Task.Delay(Timeout.Infinite, CancellationToken);
                tcs.SetResult(0);
            }, CancellationToken, TaskCreationOptions.PreferFairness, TaskScheduler.FromCurrentSynchronizationContext());
            return new(tcs.Task);
        }
    }
}
