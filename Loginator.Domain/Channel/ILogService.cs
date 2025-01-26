// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Option;
using System;
using System.Threading;

namespace Loginator.Domain.Channel {

    public interface ILogService : IAsyncDisposable {

        CancellationToken Token { get; }

        ILogWriter CreateWriter(Connection connection);

        void StartWriter(ILogWriter logWriter);

        void StopWriter(ILogWriter logWriter);

        void RemoveWriter(ILogWriter logWriter);
    }
}
