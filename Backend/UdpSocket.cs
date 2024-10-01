// Copyright (C) 2024 Claudia Wagner

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Backend {

    public class UdpSocket : ISocket {

        private readonly Socket udpSocket;

        public UdpSocket() =>
            udpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        public AddressFamily AddressFamily =>
            udpSocket.AddressFamily;

        public void Bind(EndPoint localEp) =>
            udpSocket.Bind(localEp);

        public void Close() =>
            udpSocket.Close();

        public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, SocketAddress remoteEndpoint, CancellationToken ct = default) =>
            udpSocket.ReceiveFromAsync(buffer, socketFlags, remoteEndpoint, ct);
    }
}
