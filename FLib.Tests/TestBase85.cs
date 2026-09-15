using System;
using System.Text;

namespace FLib.Tests
{
    public class TestBase85
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("Man ", "9jqo^")]
        [InlineData("Hello, world!", "87cURD_*#TDfTZ)+T")]
        [InlineData("M", "9`")]
        [InlineData("Ma", "9jn")]
        [InlineData("Man", "9jqo")]
        [InlineData("    ", "+<VdL")]
        public void Ascii85KnownVectors(string plain, string encoded)
        {
            var bytes = Encoding.ASCII.GetBytes(plain);
            Assert.Equal(encoded, Base85.Ascii85.Encode(bytes));
            Assert.Equal(bytes, Base85.Ascii85.Decode(encoded));
        }

        [Fact]
        public void Z85KnownVector()
        {
            var bytes = new byte[] { 0x86, 0x4f, 0xd2, 0x6f, 0xb5, 0x59, 0xf7, 0x5b };
            Assert.Equal("HelloWorld", Base85.Z85.Encode(bytes));
            Assert.Equal(bytes, Base85.Z85.Decode("HelloWorld"));
        }

        [Fact]
        public void ShortcutsAndWhitespace()
        {
            var coder = new Base85(Base85.Ascii85.Alphabet, 'z', 'y');
            var bytes = new byte[] { 0, 0, 0, 0, 32, 32, 32, 32, 0 };
            Assert.Equal("zy!!", coder.Encode(bytes));
            Assert.Equal(bytes, coder.Decode(" \tz\r\ny\u0085!\u00a0! "));
            Assert.Empty(coder.Decode(" \t\r\n\v\f\u0085\u00a0"));
            Assert.Equal("!!", coder.Encode(new byte[1]));
            Assert.Equal("!!!", coder.Encode(new byte[2]));
            Assert.Equal("!!!!", coder.Encode(new byte[3]));
            Assert.False(coder.TryDecode("!y", new byte[8], out _));
            Assert.False(coder.TryDecode("y!", new byte[8], out var written));
            Assert.Equal(4, written);
        }

        [Theory]
        [InlineData("!")]
        [InlineData("!!!!! !")]
        [InlineData("!z")]
        [InlineData("!!!!z")]
        [InlineData("uuuuu")]
        [InlineData("s8W-\"")]
        [InlineData("uu")]
        [InlineData("uuu")]
        [InlineData("uuuu")]
        [InlineData("\uffff")]
        [InlineData("\0")]
        [InlineData("<~z~>")]
        public void RejectsMalformedText(string text)
        {
            Assert.False(Base85.Ascii85.TryDecode(text, new byte[64], out _));
            Assert.Throws<FormatException>(() => Base85.Ascii85.Decode(text));
        }

        [Fact]
        public void RandomRoundTripsAndCapacityBoundaries()
        {
            var random = new Random(85);
            var custom = new Base85(Base85.Ascii85.Alphabet, 'z', 'y');
            foreach (var coder in new[] { Base85.Ascii85, Base85.Z85, custom })
            {
                for (var length = 0; length <= 1024; length++)
                {
                    var bytes = new byte[length];
                    random.NextBytes(bytes);
                    if (length % 7 == 0)
                        Array.Fill(bytes, (byte)0xff);
                    var encoded = coder.Encode(bytes);
                    Assert.True(encoded.Length <= coder.GetSafeCharCountForEncoding(bytes));
                    Assert.True(length <= coder.GetSafeByteCountForDecoding(encoded));
                    Assert.Equal(bytes, coder.Decode(encoded));

                    var chars = new char[encoded.Length];
                    Assert.True(coder.TryEncode(bytes, chars, out var charsWritten));
                    Assert.Equal(encoded.Length, charsWritten);
                    Assert.Equal(encoded, new string(chars));
                    var output = new byte[length];
                    Assert.True(coder.TryDecode(chars, output, out var bytesWritten));
                    Assert.Equal(length, bytesWritten);
                    Assert.Equal(bytes, output);
                    if (length == 0)
                        continue;
                    Assert.False(coder.TryEncode(bytes, chars.AsSpan(0, chars.Length - 1), out _));
                    Assert.False(coder.TryDecode(chars, output.AsSpan(0, length - 1), out _));
                }
            }
        }

        [Fact]
        public void FailurePreservesWholeBlockPrefixAndBufferBounds()
        {
            var chars = new[] { '?', '?', '?', '?', '?', '?' };
            Assert.False(Base85.Ascii85.TryEncode(new byte[5], chars.AsSpan(1, 2), out var charsWritten));
            Assert.Equal(1, charsWritten);
            Assert.Equal("?z????", new string(chars));
            var bytes = new byte[8];
            Array.Fill(bytes, (byte)0xcc);
            Assert.False(Base85.Ascii85.TryDecode("z!!", bytes.AsSpan(1, 4), out var bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xcc, 0, 0, 0, 0, 0xcc, 0xcc, 0xcc }, bytes);
        }

        [Fact]
        public void ValidatesAlphabetAndSupportsUnicode()
        {
            Assert.Throws<ArgumentNullException>(() => new Base85(null));
            Assert.Throws<ArgumentException>(() => new Base85("short"));
            Assert.Throws<ArgumentException>(() => new Base85(new string('a', 85)));
            Assert.Throws<ArgumentException>(() => new Base85(" " + Base85.Ascii85.Alphabet.Substring(1)));
            Assert.Throws<ArgumentException>(() => new Base85(Base85.Ascii85.Alphabet, '!'));
            Assert.Throws<ArgumentException>(() => new Base85(Base85.Ascii85.Alphabet, ' '));
            Assert.Throws<ArgumentException>(() => new Base85(Base85.Ascii85.Alphabet, 'z', 'z'));
            var alphabet = new char[85];
            for (var i = 0; i < alphabet.Length; i++)
                alphabet[i] = (char)(0xffab + i);
            var coder = new Base85(new string(alphabet));
            var input = new byte[] { 0xff, 0xff, 0xff, 0xff, 42 };
            Assert.Equal(input, coder.Decode(coder.Encode(input)));
        }
    }
}
