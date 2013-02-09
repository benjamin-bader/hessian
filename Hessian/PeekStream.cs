using System;
using System.IO;

namespace Hessian
{
    public class PeekStream : Stream
    {
        private Stream inner;
        private byte? peek;

        public PeekStream(Stream inner)
        {
            if (inner == null) {
                throw new ArgumentNullException("inner");
            }

            this.inner = inner;
            this.peek = null;
        }

        public override bool CanRead {
            get {
                return inner.CanRead;
            }
        }

        public override bool CanSeek {
            get {
                return false;
            }
        }

        public override bool CanWrite {
            get {
                return false;
            }
        }

        public override long Length {
            get {
                return inner.Length;
            }
        }

        public override long Position {
            get {
                return inner.Position - (peek.HasValue ? 1 : 0);
            }
            set {
                throw new NotSupportedException("Seeking not supported.");
            }
        }

        public byte? Peek ()
        {
            if (!peek.HasValue) {
                var b = inner.ReadByte();

                if (b == -1) {
                    return null;
                }

                peek = (byte) b;
            }

            return peek;
        }

        public override int ReadByte ()
        {
            if (peek.HasValue) {
                var val = peek.Value;
                peek = null;
                return val;
            }

            return inner.ReadByte();
        }

        public override int Read (byte[] buffer, int offset, int count)
        {
            if (buffer == null) {
                throw new ArgumentNullException ();
            }

            if (offset < 0 || offset >= buffer.Length) {
                throw new ArgumentOutOfRangeException ("offset", offset, "Offset outside the bounds of the given buffer."); 
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException ("count", count, "Count cannot be less than zero.");
            }

            if (offset + count >= buffer.Length) {
                throw new ArgumentException ("Buffer not big enough to contain the requested data at the given offset.");
            }

            if (count == 0) {
                return 0;
            }

            var bytesToRead = count;

            if (peek.HasValue) {
                buffer[offset++] = peek.Value;
                peek = null;
                --bytesToRead;
            }

            int bytesRead;
            while (bytesToRead > 0 && (bytesRead = inner.Read (buffer, offset, bytesToRead)) != 0) {
                offset += bytesRead;
                bytesToRead -= bytesRead;
            }

            return count - bytesToRead;
        }

        public override void Write (byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Writes not supported.");
        }

        public override void SetLength (long value)
        {
            throw new NotSupportedException("Seeking not supported.");
        }

        public override long Seek (long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seeking not supported.");
        }

        public override void Flush ()
        {
            throw new NotSupportedException("Writes not supported.");
        }

        protected override void Dispose (bool disposing)
        {
            if (inner != null) {
                inner.Dispose ();
                inner = null;
            }

            base.Dispose (disposing);
        }
    }
}

