// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Model;
using System.Collections.Generic;
using System.Threading;

namespace Backend {

    public interface IReceiver {

        IAsyncEnumerable<Log> ReadAsync(int port, CancellationToken cancelToken);
    }
}
