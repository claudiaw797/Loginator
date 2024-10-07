// Copyright (C) 2024 Claudia Wagner

using System;
using System.Buffers;
using System.IO;

namespace Backend.Server {

    internal sealed class PooledBytes : IDisposable {

        private readonly int length;
        private readonly byte[] bytes;

        private PooledBytes(int length) {
            this.length = length;
            bytes = ArrayPool<byte>.Shared.Rent(length);
        }

        public Stream AsStream() => new MemoryStream(bytes, 0, length);

        public byte[] ToArray() {
            var copy = new byte[length];
            Array.Copy(bytes, copy, length);
            return copy;
        }

        public void Dispose() => ArrayPool<byte>.Shared.Return(bytes);

        public static implicit operator byte[](PooledBytes b) => b.bytes;

        public static implicit operator Stream(PooledBytes b) => b.AsStream();

        public static PooledBytes Rent(int minimumLength) => new(minimumLength);
    }
}
