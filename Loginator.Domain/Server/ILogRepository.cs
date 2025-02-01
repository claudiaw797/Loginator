// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Loginator.Domain.Server {

    public interface ILogRepository : IDisposable {

        bool IsActive { get; set; }

        IAsyncEnumerable<Log> GetEnumerableAsync(int port, string? ipAddress, CancellationToken ct);
    }
}
