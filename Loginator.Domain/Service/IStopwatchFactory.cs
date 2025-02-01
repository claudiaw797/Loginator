// Copyright (C) 2025 Claudia Wagner

namespace Loginator.Domain.Service {

    public interface IStopwatchFactory {

        IStopwatch CreateStopwatch(bool enabled);
    }
}
