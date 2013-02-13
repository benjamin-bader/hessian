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
    public class DeserializerTests
    {
        // SHORT STRINGS
        // A short string is a string of 0-31 characters, encoded as (length) utf-8

        [Test]
        public void ReadValue_AccountsForAllByteValues()
        {
            for (var i = 0; i <= byte.MaxValue; ++i) {
                var data = new byte[] {(byte) i};
                var stream = new MemoryStream(data);
                var ds = new Deserializer(stream);

                try {
                    ds.ReadValue();
                }
                catch (Exception ex) {
                    Assert.IsFalse(ex.Message.StartsWith("WTF"), ex.Message);
                }
            }
        }

        [Test]
        public void ReadString_ReadsCompactEmptyString()
        {
            var data = new byte[] { 0x00 };
            var stream = new MemoryStream(data);
            var ds = new Deserializer(stream);

            var str = ds.ReadString();

            Assert.AreEqual(string.Empty, str);
        }

        [Test]
        public void ReadString_ReadsLongestCompactString()
        {
            // Should be 31 characters...?
            var ramayana = "गोस्वामी तुलसीदासजी कृत महाकाव.";
            var encoded = Encoding.UTF8.GetBytes(ramayana);
            var stream = new MemoryStream(encoded.Length + 1);
            stream.WriteByte(31);
            stream.Write(encoded, 0, encoded.Length);
            stream.Position = 0;
            var ds = new Deserializer(stream);

            var str = ds.ReadString();

            Assert.AreEqual(ramayana, str);
        }

        // CLASS DEFS
        //

        [Test]
        public void ReadClassDef_Works()
        {
            var name = new byte[] { 0x06, (byte)'s', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e' };
            var fields = new[]
            {
                new byte[] { 0x03, (byte)'f', (byte)'o', (byte)'o' },
                new byte[] { 0x03, (byte)'b', (byte)'a', (byte)'r' }
            };

            var stream = new MemoryStream();
            stream.WriteByte((byte)'C');
            stream.Write(name, 0, name.Length);
            stream.WriteByte(0x92); // two fields
            stream.Write(fields[0], 0, fields[0].Length);
            stream.Write(fields[1], 0, fields[1].Length);
            stream.Position = 0;
            var ds = new Deserializer(stream);

            var def = ds.ReadClassDefinition();

            Assert.AreEqual("sample", def.Name);
            Assert.AreEqual(2, def.Fields.Length);
            CollectionAssert.AreEqual(new[] {"foo", "bar"}, def.Fields);
        }

        [Test, ExpectedException(typeof(UnexpectedTagException))]
        public void ReadClassDef_WithoutTag_Throws()
        {
            var name = new byte[] { 0x06, (byte)'s', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e' };
            var fields = new[]
            {
                new byte[] { 0x03, (byte)'f', (byte)'o', (byte)'o' },
                new byte[] { 0x03, (byte)'b', (byte)'a', (byte)'r' }
            };

            var stream = new MemoryStream();
            stream.Write(name, 0, name.Length);
            stream.WriteByte(0x92); // two fields
            stream.Write(fields[0], 0, fields[0].Length);
            stream.Write(fields[1], 0, fields[1].Length);
            stream.Position = 0;
            new Deserializer(stream).ReadClassDefinition();
        }
    }
}
