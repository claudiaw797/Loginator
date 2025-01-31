// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Option;
using Loginator.Domain.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Domain.Channel {

    public interface ILogWriter : IAsyncDisposable {

        Connection Connection { get; }

        Task Task { get; }

        CancellationToken Token { get; }

        bool IsActive { get; set; }

        Task StartAsync(ILogRepositoryFactory logRepositoryFactory, CancellationToken ct);

        Task StopAsync(CancellationToken ct);

        Task CloseAsync();
    }
}
