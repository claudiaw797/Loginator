// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Model;
using System.Collections.Generic;

namespace Loginator.Domain.Channel {

    public interface ILogProcessor {

        void ProcessLogs(IEnumerable<Log> logs);
    }
}
