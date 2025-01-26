// Copyright (C) 2024 Claudia Wagner

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Server {

    internal abstract class AbstractSocket {

        private static readonly byte[] PingBytes = Encoding.UTF8.GetBytes("ping");

        protected readonly Socket socket;

        protected AbstractSocket(Socket socket) {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.socket = socket;
        }

        public bool IsBound =>
            socket.IsBound;

        public static implicit operator Socket?(AbstractSocket s) =>
            s?.socket;

        public virtual ValueTask<AbstractSocket> AcceptAsync(CancellationToken ct) =>
            new(this);

        public virtual void Bind(EndPoint localEp) =>
            socket.Bind(localEp);

        public virtual void Close() =>
            socket.Close();

        public virtual void Dispose() =>
            socket.Dispose();

        public virtual void Listen() =>
            socket.Listen();

        public virtual ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken ct = default) =>
            socket.ReceiveAsync(buffer, socketFlags, ct);

        public virtual async Task<bool> IsConnectedAsync(Socket? socket, CancellationToken ct) {
            if (socket is null) return false;

            int count = await socket.SendAsync(PingBytes, ct).ConfigureAwait(false);
            return count == PingBytes.Length && socket.Connected;
        }
    }
}
