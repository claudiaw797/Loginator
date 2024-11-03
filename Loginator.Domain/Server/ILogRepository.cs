// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Domain.Model;
using System.Collections.Generic;
using System.Threading;

namespace Loginator.Domain.Server {

    public interface ILogRepository {

        IAsyncEnumerable<Log> GetEnumerableAsync(int port, CancellationToken cancelToken);
    }
}
