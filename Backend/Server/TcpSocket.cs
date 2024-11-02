// Copyright (C) 2024 Claudia Wagner

using System.Net.Sockets;

namespace Loginator.Infrastructure.Server {

    internal sealed class TcpSocket : AbstractSocket {

        public TcpSocket() : base(new Socket(SocketType.Stream, ProtocolType.Tcp)) { }

        private TcpSocket(Socket socket) : base(socket) { }

        public override AbstractSocket Accept() =>
            new TcpSocket(socket.Accept());
    }
}
