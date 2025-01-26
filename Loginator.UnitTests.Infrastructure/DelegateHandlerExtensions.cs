// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FakeItEasy.Configuration;
using System;

namespace Loginator.UnitTests.Infrastructure {

    public static class DelegateHandlerExtensions {

        public static IVoidArgumentValidationConfiguration CallToClose(this IDelegateHandler delegateHandler) =>
            A.CallTo(() => delegateHandler.Close());

        public static IVoidArgumentValidationConfiguration CallToError(this IDelegateHandler delegateHandler) =>
            A.CallTo(() => delegateHandler.Error(A<string>._, A<Exception>._));

        public static IVoidArgumentValidationConfiguration CallToError(this IDelegateHandler delegateHandler, Exception expected) =>
            A.CallTo(() => delegateHandler.Error(A<string>._, A<Exception>.That.Matches(ex => ex == expected)));
    }
}