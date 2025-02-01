// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Service;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Loginator.Infrastructure.Service {

    internal class StopwatchFactory(IServiceProvider serviceProvider) : IStopwatchFactory {

        public IStopwatch CreateStopwatch(bool enabled) =>
            serviceProvider.GetRequiredKeyedService<IStopwatch>(enabled);
    }
}
