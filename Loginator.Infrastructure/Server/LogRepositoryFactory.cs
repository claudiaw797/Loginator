// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Option;
using Loginator.Domain.Server;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Loginator.Infrastructure.Server {

    internal class LogRepositoryFactory(IServiceProvider serviceProvider) : ILogRepositoryFactory {

        public ILogRepository CreateLogRepository(ConnectionType connectionType, LogType logType) =>
            serviceProvider.GetRequiredKeyedService<ILogRepository>((connectionType, logType));
    }
}
