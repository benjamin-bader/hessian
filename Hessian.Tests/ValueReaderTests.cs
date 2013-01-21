using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Hessian.Tests
{
    [TestFixture]
    public class ValueReaderTests
    {
        private uint ReadCodepoint(params byte[] data)
        {
            var stream = new MemoryStream(data);
            var reader = new ValueReader(stream);
            return reader.ReadUtf8Codepoint();
        }

        [Test]
        public void Peek_DoesNotConsumeData()
        {
            var stream = new MemoryStream(new byte[] {1, 2});
            var reader = new ValueReader(stream);

            var b0 = reader.Peek();

            Assert.IsTrue(b0.HasValue);
            Assert.AreEqual(1, b0.Value);
            Assert.AreEqual(b0, reader.ReadByte());
        }

        [Test]
        public void ReadUtf8Codepoint_ReadsAscii()
        {
            var codepoint = ReadCodepoint((byte) 'a');
            Assert.AreEqual('a', codepoint);
        }

        [Test]
        public void ReadUtf8Codepoint_RejectsOverlongEncodings()
        {
            var overlongSlashes = new[]
            {
                new byte[] { 0xC0, 0xAF },
                new byte[] { 0xE0, 0x80, 0xAF },
                new byte[] { 0xF0, 0x80, 0x80, 0xAF }
            };

            foreach (var encoding in overlongSlashes) {
                var codepoint = ReadCodepoint(encoding);
                Assert.AreEqual(0xFFFD, codepoint, "Decoding failed on sequence {0}", encoding.Length);
            }
        }

        [Test]
        public void ReadUtf8Codepoint_RejectsBoundaryOverlongEncodings()
        {
            // These are the maximum representable overlong encodings.  They should all be rejected.
            var overlongs = new[]
            {
                new byte[] { 0xC1, 0xBF },
                new byte[] { 0xE0, 0x9F, 0xBF },
                new byte[] { 0xF0, 0x8F, 0xBF, 0xBF }
            };

            foreach (var encoding in overlongs) {
                var codepoint = ReadCodepoint(encoding);
                Assert.AreEqual(0xFFFD, codepoint, "Decoding failed on overlong boundary sequence length {0}", encoding.Length);
            }
        }

        [Test]
        public void ReadUtf8Codepoint_RejectsContinuationBytesInInitialPosition()
        {
            var continuations = Enumerable.Range(0x80, 64).Select(i => (byte)i).ToArray();
            var stream = new MemoryStream(continuations);
            var reader = new ValueReader(stream);

            for (var i = 0; i < continuations.Length; ++i) {
                Assert.AreEqual(0xFFFD, reader.ReadUtf8Codepoint());
            }
        }

        [Test]
        public void ReadUtf8Codepoint_RejectsLonelyStartCharacters()
        {
            Action<IEnumerable<byte[]>> assert = encodings => {
                foreach (var encoding in encodings) {
                    var codepoint = ReadCodepoint(encoding);
                    Assert.AreEqual(0xFFFD, codepoint, "Decoding failed on lonely start length {0}", encoding.Length);
                }
            };

            var twoByteStarts = Enumerable.Range(0xC0, 32).Select(i => new byte[] { (byte)i, 0x20 });
            var threeByteStarts = Enumerable.Range(0xE0, 16).Select(i => new byte[] { (byte)i, 0x20, 0x20 });
            var fourByteStarts = Enumerable.Range(0xF0, 8).Select(i => new byte[] { (byte)i, 0x20, 0x20, 0x20 });

            assert(twoByteStarts);
            assert(threeByteStarts);
            assert(fourByteStarts);
        }

        [Test]
        public void ReadUtf8Codepoint_RejectsImpossibleBytes()
        {
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xFE));
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xFF));
        }

        [Test]
        public void ReadUtf8Codepoint_RejectsUtf16Surrogates()
        {
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xED, 0xA0, 0x80));
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xED, 0xAD, 0xBF));
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xED, 0xAE, 0x80));
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xED, 0xAF, 0xBF));
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xED, 0xB0, 0x80));
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xED, 0xBE, 0x80));
            Assert.AreEqual(0xFFFD, ReadCodepoint(0xED, 0xBF, 0xBF));
        }
    }
}
