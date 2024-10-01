// Copyright (C) 2024 Claudia Wagner

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Backend {

    public interface ISocket {

        AddressFamily AddressFamily { get; }

        void Bind(EndPoint localEp);
        void Close();

        ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, SocketAddress receivedAddress, CancellationToken cancellationToken = default);
    }
}
