using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpectBetter;
using NUnit.Framework;

using Hessian.Platform;

namespace Hessian.Tests.Platform
{
    [TestFixture]
    public class BigEndianConverterTests
    {
        // As a little-endian int, this is 67305985.
        // As a big-endian int, this is 16909060.
        private static readonly byte[] INT32 = new byte[] {0x01, 0x02, 0x03, 0x04};
        private static readonly byte[] TWELVE_POINT_TWO_FIVE_DOUBLE = new byte[] {0x40, 0x28, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00};

        private BigEndianBitConverter converter;

        [SetUp]
        public void Setup()
        {
            converter = new BigEndianBitConverter();
        }

        [Test]
        public void ToBoolean_WithOne_ReturnsTrue()
        {
            Expect.The(converter.ToBoolean(new byte[] {0x01}, 0)).ToBeTrue();
        }

        [Test]
        public void ToBoolean_WithZero_ReturnsFalse()
        {
            Expect.The(converter.ToBoolean(new byte[] {0x00}, 0)).ToBeFalse();
        }

        [Test]
        public void ToBoolean_WithOther_DoesWhatNow()
        {
            Expect.The(converter.ToBoolean(new byte[] {0xFF}, 0)).ToBeTrue();
        }

        [Test]
        public void ToChar_WithNetworkOrderAscii_Works()
        {
            var value = converter.ToChar(new byte[] {0x00, (byte) 'A'}, 0);
            Expect.The(value).ToEqual('A');
        }

        [Test]
        public void ToChar_WithLittleEndianAscii_Fails()
        {
            var value = converter.ToChar(new byte[] {(byte) 'Z', 0x00}, 0);
            Expect.The(value).Not.ToEqual('Z');
        }

        [Test]
        public void ToDouble_WithNetworkOrderBytes_Works()
        {
            var value = converter.ToDouble(TWELVE_POINT_TWO_FIVE_DOUBLE, 0);
            Expect.The(value).ToEqual(12.25);
        }

        [Test]
        public void ToDouble_WithLittleEndianBytes_Fails()
        {
            var value = converter.ToDouble(TWELVE_POINT_TWO_FIVE_DOUBLE.Reverse().ToArray(), 0);
            Expect.The(value).Not.ToEqual(12.25);
        }

        [Test]
        public void GetBytes_WithDouble_YieldsNetworkOrderBytes()
        {
            var bytes = converter.GetBytes(12.25D);
            Expect.The(bytes).ToContainExactly(TWELVE_POINT_TWO_FIVE_DOUBLE);
        }

        [Test]
        public void ToInt32_WithNetworkOrderBytes_Works()
        {
            var value = converter.ToInt32(INT32, 0);
            Expect.The(value).ToEqual(16909060);
        }

        public void ToInt32_WithLittleEndianBytes_Fails()
        {
            var value = converter.ToInt32(INT32.Reverse().ToArray(), 0);
            Expect.The(value).Not.ToEqual(16909060);
        }
    }
}
