// Copyright (C) 2024 Claudia Wagner

using Backend.Model;
using FakeItEasy.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Backend.UnitTests.Converter.ChainsawToLogConverterTestData;

namespace Backend.UnitTests {

    /// <summary>
    /// Represents test data for <see cref="ReceiverTests"/>.
    /// </summary>
    internal class ReceiverTestData {

        public static string ValidLogMessage() {
            var input = Log4JDefault(false, false, false, SaveOptions.None);
            return input;
        }

        public static IEnumerable<string> ValidLogMessages() {
            bool[] booleans = [true, false];

            foreach (var hasPrefix in booleans) {
                foreach (var hasNamespace in booleans) {
                    foreach (var isMixed in booleans) {
                        foreach (var option in FormatOptions) {
                            var input = Log4JDefault(hasPrefix, hasNamespace, isMixed, option);
                            yield return input;
                        }
                    }
                }
            }
        }

        public static Log ValidLog =>
            LogFromValidLog4jXml;

        public class SocketServer {

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
                    throw new InvalidOperationException("Cancellation token is already canceled, method should not be called anymore.");
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

                var memory = Encoding.UTF8.GetBytes(returnValues[callCounter++]).AsMemory();
                var argument = call.Arguments.Get<Memory<byte>>(0);
                memory.CopyTo(argument);
                return new(memory.Length);
            }

            public ValueTask<int> WaitUntilCanceled(IFakeObjectCall call) {
                var tcs = new TaskCompletionSource<int>();
                Task.Factory.StartNew(async () => {
                    await Task.Delay(Timeout.Infinite, CancellationToken);
                    tcs.SetResult(0);
                }, CancellationToken, TaskCreationOptions.PreferFairness, TaskScheduler.FromCurrentSynchronizationContext());
                return new(tcs.Task);
            }
        }
    }
}