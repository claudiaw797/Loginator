// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Option;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Domain.Channel {

    public interface ILogService : IAsyncDisposable {

        CancellationToken Token { get; }

        ILogWriter CreateWriter(Connection connection);

        Task StartWriterAsync(ILogWriter logWriter);

        Task StopWriterAsync(ILogWriter logWriter);

        Task RemoveWriterAsync(ILogWriter logWriter);
    }
}
