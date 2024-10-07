// Copyright (C) 2024 Claudia Wagner

using Loginator.Controls;
using System;
using System.Windows.Threading;

namespace Loginator.UnitTests {

    internal class DispatcherMock : IDispatcher {

        public Dispatcher? UIDispatcher { get; private set; }

        public void CheckBeginInvokeOnUI(Action action) =>
            action();

        public DispatcherOperation RunAsync(Action action) =>
            UIDispatcher!.BeginInvoke(action);

        public void Initialize() { }

        public void Reset() { }
    }
}
