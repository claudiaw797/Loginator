// Copyright (C) 2024 Claudia Wagner

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Server {

    internal sealed class UdpSocket : AbstractSocket {

        public UdpSocket() : base(new Socket(SocketType.Dgram, ProtocolType.Udp)) { }

        public override void Listen() { }

        public override Task<bool> IsConnected(Socket s, CancellationToken _) =>
            Task.FromResult(true);
    }
}
