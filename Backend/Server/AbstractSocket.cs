// Copyright (C) 2024 Claudia Wagner

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loginator.Infrastructure.Server {

    internal abstract class AbstractSocket(Socket socket) {

        private static readonly byte[] PingBytes = Encoding.UTF8.GetBytes("ping");

        protected readonly Socket socket = socket;

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
            int count = await socket.SendAsync(PingBytes, cancelToken);
            return count == PingBytes.Length && socket.Connected;
        }
    }
}
