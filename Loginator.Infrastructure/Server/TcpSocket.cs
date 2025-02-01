// Copyright (C) 2024 Claudia Wagner

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Server {

    internal sealed class TcpSocket : AbstractSocket {

        public TcpSocket() : base(new Socket(SocketType.Stream, ProtocolType.Tcp)) { }

        private TcpSocket(Socket socket) : base(socket) { }

        public async override ValueTask<AbstractSocket> AcceptAsync(CancellationToken ct) =>
            new TcpSocket(await socket.AcceptAsync(ct).ConfigureAwait(false));
    }
}
