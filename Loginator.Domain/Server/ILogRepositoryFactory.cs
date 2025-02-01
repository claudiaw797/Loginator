// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Option;

namespace Loginator.Domain.Server {

    public interface ILogRepositoryFactory {

        ILogRepository CreateLogRepository(ConnectionType connectionType, LogType logType);
    }
}
