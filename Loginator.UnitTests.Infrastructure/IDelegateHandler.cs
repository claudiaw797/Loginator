// Copyright (C) 2025 Claudia Wagner

using System;

namespace Loginator.UnitTests.Infrastructure {

    public interface IDelegateHandler {

        void Close();

        void Error(string actionKey, Exception exception);
    }
}