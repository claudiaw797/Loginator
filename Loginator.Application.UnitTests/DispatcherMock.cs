// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Service;
using System;

namespace Loginator.Application.UnitTests {

    internal class DispatcherMock : IDispatcher {

        public void BeginInvokeOnUIThread(Action action) =>
            action();

        public void Initialize() { }
    }
}
