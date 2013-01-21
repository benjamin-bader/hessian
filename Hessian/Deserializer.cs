using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hessian
{
    public class Deserializer
    {
        private readonly ValueReader reader;
        private readonly List<string[]> classDefs;
        private readonly List<object> objectRefs;

        public Deserializer (Stream stream)
        {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }

            reader = new ValueReader(stream);
            classDefs = new List<string[]>();
            objectRefs = new List<object>();
        }


        // XXX Moving to Read<Type> methods, and not speciaized Read<Form><Type>.
        public object ReadObject ()
        {
            var tag = reader.Peek ();

            if (!tag.HasValue) {
                throw new EndOfStreamException ();
            }

            switch (tag.Value) {
                case 0x00: case 0x01: case 0x02: case 0x03: case 0x04: case 0x05: case 0x06: case 0x07:
                case 0x08: case 0x09: case 0x0A: case 0x0B: case 0x0C: case 0x0D: case 0x0E: case 0x0F:
                case 0x10: case 0x11: case 0x12: case 0x13: case 0x14: case 0x15: case 0x16: case 0x17:
                case 0x18: case 0x19: case 0x1A: case 0x1B: case 0x1C: case 0x1D: case 0x1E: case 0x1F:
                    return ReadShortString();
               
                case 0x20: case 0x21: case 0x22: case 0x23: case 0x24: case 0x25: case 0x26: case 0x27:
                case 0x28: case 0x29: case 0x2A: case 0x2B: case 0x2C: case 0x2D: case 0x2E: case 0x2F:
                    return ReadShortBinary();

                case 0x30: case 0x31: case 0x32: case 0x33:
                    return ReadMediumString();

                case 0x34: case 0x35: case 0x36: case 0x37:
                    return ReadMediumBinary();

                case 0x38: case 0x39: case 0x3A: case 0x3B: case 0x3C: case 0x3D: case 0x3E: case 0x3F:
                    return ReadThreeByteLong();

                case 0x40:
                    return Reserved();

                case 0x41: case 0x42:
                    return ReadChunkedBinary();

                case 0x43:
                    return ReadClassDefinition();

            }

            throw new NotImplementedException();
        }

        public object Reserved ()
        {
            reader.ReadByte();
            return null;
        }

        public string ReadString()
        {
            var tag = reader.Peek();

            if (!tag.HasValue) {
                throw new EndOfStreamException();
            }

            if (tag.Value < 0x20) {
                return ReadShortString();
            }

            if (tag.Value >= 0x30 && tag.Value <= 0x33) {
                return ReadMediumString();
            }

            if (tag.Value == 'R' || tag.Value == 'S') {
                return ReadChunkedString();
            }

            throw new InvalidDataException();
        }

        public string ReadShortString ()
        {
            var length = reader.ReadByte();
            return ReadStringWithLength(length);
        }

        public string ReadMediumString ()
        {
            var b0 = reader.ReadByte ();
            var b1 = reader.ReadByte ();
            var length = ((b0 - 0x30) << 8) | b1;
            return ReadStringWithLength(length);
        }

        public string ReadStringWithLength (int length)
        {
            var sb = new StringBuilder (length);
            while (length-- > 0) {
                sb.AppendCodepoint(reader.ReadUtf8Codepoint());
            }
            return sb.ToString();
        }

        private string ReadChunkedString()
        {
            var sb = new StringBuilder();
            var final = false;

            while (!final) {
                var tag = reader.ReadByte();
                final = tag == 'S';
                var length = reader.ReadShort();
                while (length-- > 0) {
                    sb.AppendCodepoint(reader.ReadUtf8Codepoint());
                }
            }

            return sb.ToString();
        }

        public byte[] ReadShortBinary ()
        {
            var length = reader.ReadByte();
            var data = new byte[length];
            reader.Read(data, length);
            return data;
        }

        public byte[] ReadMediumBinary()
        {
            var b0 = reader.ReadByte();
            var b1 = reader.ReadByte();
            var length = ((b0 - 0x34) << 8) | b1;
            var data = new byte[length];
            reader.Read(data, length);
            return data;
        }

        public long ReadThreeByteLong()
        {
            var b0 = reader.ReadByte();
            var b1 = reader.ReadByte();
            var b2 = reader.ReadByte();

            return ((b0 - 0x3CL) << 16) | (b1 << 8) | b2;
        }

        public byte[] ReadChunkedBinary()
        {
            var data = new List<byte>();
            var final = false;

            while (!final) {
                var tag = reader.ReadByte();
                final = tag == 'B';
                var length = reader.ReadShort();
                var buff = new byte[length];
                reader.Read(buff, length);
                data.AddRange(buff);
            }

            return data.ToArray();
        }

        public int ReadInteger()
        {
            return 0;
        }

        public ClassDef ReadClassDefinition()
        {
            var tag = reader.ReadByte();
            var name = ReadString();
            var classRef = classDefs.Count;
            var fieldCount = ReadInteger();
            var fields = new string[fieldCount];
            for (var i = 0; i < fields.Length; ++i) {
                fields[i] = ReadString();
            }

            classDefs.Add(fields);

            return new ClassDef(classRef, name, fields);
        }
    }
}

