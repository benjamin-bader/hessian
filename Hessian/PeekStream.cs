using System;
using System.Collections.Generic;
using System.IO;

namespace Hessian
{
    public class PeekStream : Stream
    {
        private Stream inner;
        private Stack<byte> stack;

        public PeekStream(Stream inner)
        {
            if (inner == null) {
                throw new ArgumentNullException("inner");
            }

            this.inner = inner;
            this.stack = new Stack<byte>();
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
                return inner.Position - stack.Count;
            }
            set {
                throw new NotSupportedException("Seeking not supported.");
            }
        }

        public byte? Peek ()
        {
            if (stack.Count > 0) {
                return stack.Peek ();
            }

            var b = inner.ReadByte ();

            if (b == -1) {
                return null;
            }

            stack.Push ((byte)b);
            return (byte)b;
        }

        public override int ReadByte ()
        {
            if (stack.Count > 0) {
                return stack.Pop ();
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

            while (bytesToRead > 0 && stack.Count > 0) {
                buffer [offset++] = stack.Pop ();
            }

            var bytesRead = 0;
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

            stack = null;

            base.Dispose (disposing);
        }
    }
}

