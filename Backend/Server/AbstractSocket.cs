// Copyright (C) 2024 Claudia Wagner

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.Server {

    internal abstract class AbstractSocket {

        private static readonly byte[] PING_BYTES = Encoding.UTF8.GetBytes("ping");

        protected readonly Socket socket;

        protected AbstractSocket(Socket socket) =>
            this.socket = socket;

        public static implicit operator Socket(AbstractSocket s) =>
            s.socket;

        public virtual AbstractSocket Accept() =>
            this;

        public virtual void Bind(EndPoint localEp) =>
            socket.Bind(localEp);

        public virtual void Close() =>
            socket.Close();

        public virtual void Dispose() =>
            socket.Dispose();

        public virtual void Listen() =>
            socket.Listen();

        public virtual ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancelToken = default) =>
            socket.ReceiveAsync(buffer, socketFlags, cancelToken);

        public virtual async Task<bool> IsConnected(Socket socket, CancellationToken cancelToken) {
            int count = await socket.SendAsync(PING_BYTES, cancelToken);
            return count == PING_BYTES.Length && socket.Connected;
        }
    }
}
