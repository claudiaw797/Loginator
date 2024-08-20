using Backend.Events;
using Backend.Model;
using System;

namespace Backend {

    public interface IReceiver {

        event EventHandler<LogReceivedEventArgs> LogReceived;

        void Initialize(Configuration configuration);
    }
}
