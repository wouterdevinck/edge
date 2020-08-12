// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iotedgeapiclient.unixsocket {

    internal class HttpBufferedStream : Stream {

        const char Cr = '\r';
        const char Lf = '\n';

        readonly BufferedStream _innerStream;

        public HttpBufferedStream(Stream stream) {
            this._innerStream = new BufferedStream(stream);
        }

        public override bool CanRead => this._innerStream.CanRead;

        public override bool CanSeek => this._innerStream.CanSeek;

        public override bool CanWrite => this._innerStream.CanWrite;

        public override long Length => this._innerStream.Length;

        public override long Position {
            get => this._innerStream.Position;
            set => this._innerStream.Position = value;
        }

        public override void Flush() {
            this._innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken) {
            return this._innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return this._innerStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return this._innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public async Task<string> ReadLineAsync(CancellationToken cancellationToken) {
            int position = 0;
            var buffer = new byte[1];
            bool crFound = false;
            var builder = new StringBuilder();
            while (true) {
                int length = await this._innerStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (length == 0) {
                    throw new IOException("Unexpected end of stream.");
                }

                if (crFound && (char) buffer[position] == Lf) {
                    builder.Remove(builder.Length - 1, 1);
                    return builder.ToString();
                }

                builder.Append((char) buffer[position]);
                crFound = (char) buffer[position] == Cr;
            }
        }

        public string ReadLine() {
            int position = 0;
            var buffer = new byte[1];
            bool crFound = false;
            var builder = new StringBuilder();
            while (true) {
                int length = this._innerStream.Read(buffer, 0, buffer.Length);
                if (length == 0) {
                    throw new IOException("Unexpected end of stream.");
                }

                if (crFound && (char) buffer[position] == Lf) {
                    builder.Remove(builder.Length - 1, 1);
                    return builder.ToString();
                }

                builder.Append((char) buffer[position]);
                crFound = (char) buffer[position] == Cr;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return this._innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            this._innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            this._innerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return this._innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing) {
            this._innerStream.Dispose();
        }

    }

}