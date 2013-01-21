using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hessian
{
    public class Deserializer
    {
        private readonly ValueReader reader;
        private readonly List<ClassDef> classDefs;
        private readonly List<object> objectRefs;

        public Deserializer (Stream stream)
        {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }

            reader = new ValueReader(stream);
            classDefs = new List<ClassDef>();
            objectRefs = new List<object>();
        }


        // XXX Moving to Read<Type> methods, and not speciaized Read<Form><Type>.
        public object ReadValue ()
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

                case 0x44:
                    return ReadDouble();

                case 0x45:
                    return Reserved();

                case 0x46:
                    return ReadBoolean();

                case 0x47:
                    return Reserved();

                case 0x48:
                    return ReadMap();

                case 0x49:
                    return ReadInteger();

                case 0x4A:
                    return ReadDateInMillis();

                case 0x4B:
                    return ReadDateInMinutes();

                case 0x5B: case 0x5C:
                    return ReadDoubleOneByte();

            }

            throw new NotImplementedException();
        }

        public object Reserved ()
        {
            reader.ReadByte();
            return ReadValue();
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

        private string ReadShortString ()
        {
            var length = reader.ReadByte();
            return ReadStringWithLength(length);
        }

        private string ReadMediumString ()
        {
            var b0 = reader.ReadByte ();
            var b1 = reader.ReadByte ();
            var length = ((b0 - 0x30) << 8) | b1;
            return ReadStringWithLength(length);
        }

        private string ReadStringWithLength (int length)
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

        private byte[] ReadShortBinary ()
        {
            var length = reader.ReadByte();
            var data = new byte[length];
            reader.Read(data, length);
            return data;
        }

        private byte[] ReadMediumBinary()
        {
            var b0 = reader.ReadByte();
            var b1 = reader.ReadByte();
            var length = ((b0 - 0x34) << 8) | b1;
            var data = new byte[length];
            reader.Read(data, length);
            return data;
        }

        public byte[] ReadBinary()
        {
            var tag = reader.Peek();
            if (!tag.HasValue) {
                throw new EndOfStreamException();
            }

            if (tag.Value >= 0x20 && tag.Value <= 0x2F) {
                return ReadShortBinary();
            }

            if (tag.Value >= 0x34 && tag.Value <= 0x37) {
                return ReadMediumBinary();
            }

            if (tag.Value == 0x41 || tag.Value == 0x42) {
                return ReadChunkedBinary();
            }

            throw new InvalidDataException(string.Format("{0:X} is not a valid binary tag.", tag.Value));
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
            var tag = reader.Peek();

            if (!tag.HasValue) {
                throw new EndOfStreamException();
            }

            // Full-length integer encoding is 'I' b0 b1 b2 b3 - i.e. a full 32-bit integer in big-endian order.
            if (tag == 0x49) {
                return ReadIntegerFull();
            }

            // Ints between -16 and 47 are encoded as value + 0x90.
            if (tag >= 0x80 && tag <= 0xBF) {
                return ReadIntegerSingleByte();
            }

            // Ints between -2048 and 2047 can be encoded as two octets with the leading byte from 0xC0 to 0xCF.
            if (tag >= 0xC0 && tag <= 0xCF) {
                return ReadIntegerTwoBytes();
            }

            // Ints between -262144 and 262143 can be three bytes with the first from 0xD0 to 0xD7.
            if (tag >= 0xD0 && tag <= 0xD7) {
                return ReadIntegerThreeBytes();
            }

            throw new InvalidDataException();
        }

        private int ReadIntegerFull()
        {
            byte tag = reader.ReadByte(),
                 b0 = reader.ReadByte(),
                 b1 = reader.ReadByte(),
                 b2 = reader.ReadByte(),
                 b3 = reader.ReadByte();

            return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }

        private int ReadIntegerSingleByte()
        {
            return reader.ReadByte() - 0x90;
        }

        private int ReadIntegerTwoBytes()
        {
            byte b0 = reader.ReadByte(),
                 b1 = reader.ReadByte();

            return ((b0 - 0xC8) << 8) | b1;
        }

        private int ReadIntegerThreeBytes()
        {
            byte b0 = reader.ReadByte(),
                 b1 = reader.ReadByte(),
                 b2 = reader.ReadByte();

            return ((b0 - 0xD4) << 16) | (b1 << 8) | b2;
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

            var classDef = new ClassDef(classRef, name, fields);
            
            classDefs.Add(classDef);

            return classDef;
        }

        public double ReadDouble()
        {
            var tag = reader.Peek();

            if (!tag.HasValue) {
                throw new EndOfStreamException();
            }

            if (tag == 0x44) {
                return ReadFullDouble();
            }

            if (tag == 0x5B || tag == 0x5C) {
                return ReadDoubleOneByte();
            }

            if (tag == 0x5D) {
                return ReadDoubleTwoBytes();
            }

            if (tag == 0x5E) {
                return ReadDoubleThreeBytes();
            }

            if (tag == 0x5F) {
                return ReadDoubleFourBytes();
            }

            return double.NaN;
        }

        private double ReadFullDouble()
        {
            var data = new byte[9]; // 9 bytes: tag + IEEE 8-byte double
            reader.Read(data, data.Length);
            return BitConverter.ToDouble(data, 1);
        }

        private double ReadDoubleOneByte()
        {
            // 0x5B encodes the double value 0.0, and 0x5C encodes 1.0.
            return reader.ReadByte() - 0x5B;
        }

        private double ReadDoubleTwoBytes()
        {
            // Doubles representing integral values between -128.0 and 127.0 are
            // encoded as single bytes.  Java bytes are signed, .NET bytes aren't,
            // so we have to cast it first.
            var tag = reader.ReadByte();
            return (sbyte) reader.ReadByte();
        }

        private double ReadDoubleThreeBytes()
        {
            // Doubles representing integral values between -32768.0 and 32767.0 are
            // encoded as two-byte integers.
            var tag = reader.ReadByte();
            return reader.ReadShort();
        }

        private double ReadDoubleFourBytes()
        {
            // Doubles that can be represented as singles are thusly encoded.
            var data = new byte[4];
            reader.Read(data, data.Length);
            return BitConverter.ToSingle(data, 0);
        }

        public bool ReadBoolean()
        {
            var tag = reader.ReadByte();

            switch (tag) {
                case 0x46: return false;
                case 0x54: return true;
            }

            throw new InvalidDataException();
        }

        public Hashtable ReadMap()
        {
            return null;
        }

        public DateTime ReadDate()
        {
            var tag = reader.Peek();

            if (!tag.HasValue) {
                throw new EndOfStreamException();
            }

            if (tag == 0x4A) {
                return ReadDateInMillis();
            }

            if (tag == 0x4B) {
                return ReadDateInMinutes();
            }

            throw new InvalidDataException();
        }

        private DateTime ReadDateInMillis()
        {
            var data = new byte[9];
            reader.Read(data, data.Length);
            var millis = BitConverter.ToInt64(data, 1);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(millis);
        }

        private DateTime ReadDateInMinutes()
        {
            var data = new byte[5];
            reader.Read(data, data.Length);
            var minutes = BitConverter.ToInt32(data, 1);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(minutes);
        }
    }
}

