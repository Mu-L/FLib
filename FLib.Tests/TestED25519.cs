using System;
using System.Numerics;
using System.Security.Cryptography;

namespace FLib.Tests
{
    public class TestED25519
    {
        // RFC 8032 section 7.1: empty, one-byte and two-byte messages.
        [Theory]
        [InlineData("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60",
            "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a", "",
            "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821591590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b")]
        [InlineData("4ccd089b28ff96da9db6c346ec114e0f5b8a319f35aba624da8cf6ed4fb8a6fb",
            "3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c", "72",
            "92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c00")]
        [InlineData("c5aa8df43f9f837bedb7442f31dcb7b166d38535076f094b85ce3a2e0b4458f7",
            "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025", "af82",
            "6291d657deec24024827e69c3abe01a30ce548a284743a445e3680d7db5ac3ac18ff9b538d16f290ae67f760984dc6594a7c15e9716ed28dc027beceea1ec40a")]
        public void Rfc8032Vectors(string seedHex, string publicKeyHex, string messageHex, string signatureHex)
        {
            var seed = Convert.FromHexString(seedHex);
            var publicKey = Convert.FromHexString(publicKeyHex);
            var message = Convert.FromHexString(messageHex);
            var signature = Convert.FromHexString(signatureHex);
            Assert.Equal(publicKey, ED25519.GetPublicKey(seed));
            Assert.Equal(signature, ED25519.Sign(message, seed));
            Assert.True(ED25519.Verify(signature, message, publicKey));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(127)]
        [InlineData(128)]
        [InlineData(129)]
        [InlineData(1023)]
        [InlineData(1048576)]
        public void RoundTripsAndSpanBounds(int length)
        {
            ED25519.CreateKeyPair(out var publicKey, out var privateKey);
            Assert.Equal(32, privateKey.Length);
            Assert.Equal(32, publicKey.Length);
            var message = new byte[length + 2];
            new Random(length).NextBytes(message);
            var seedBuffer = new byte[34];
            privateKey.CopyTo(seedBuffer, 1);
            var signatureBuffer = new byte[66];
            Array.Fill(signatureBuffer, (byte)0xcc);
            ED25519.Sign(message.AsSpan(1, length), seedBuffer.AsSpan(1, 32), signatureBuffer.AsSpan(1, 64));
            Assert.Equal(0xcc, signatureBuffer[0]);
            Assert.Equal(0xcc, signatureBuffer[65]);
            Assert.Equal(ED25519.Sign(message.AsSpan(1, length), privateKey), signatureBuffer.AsSpan(1, 64).ToArray());
            Assert.True(ED25519.Verify(signatureBuffer.AsSpan(1, 64), message.AsSpan(1, length), publicKey));
            Assert.False(ED25519.Verify(signatureBuffer.AsSpan(1, 64), message, publicKey));
            var keyBuffer = new byte[34];
            Array.Fill(keyBuffer, (byte)0xcc);
            ED25519.GetPublicKey(privateKey, keyBuffer.AsSpan(1, 32));
            Assert.Equal(publicKey, keyBuffer.AsSpan(1, 32).ToArray());
            Assert.Equal(0xcc, keyBuffer[0]);
            Assert.Equal(0xcc, keyBuffer[33]);
        }

        [Fact]
        public void RejectsTamperingAndScalarMalleability()
        {
            var seed = new byte[32];
            var message = new byte[] { 1, 2, 3 };
            var publicKey = ED25519.GetPublicKey(seed);
            var signature = ED25519.Sign(message, seed);
            for (var bit = 0; bit < 512; bit++)
            {
                signature[bit / 8] ^= (byte)(1 << (bit % 8));
                Assert.False(ED25519.Verify(signature, message, publicKey));
                signature[bit / 8] ^= (byte)(1 << (bit % 8));
            }
            for (var bit = 0; bit < 256; bit++)
            {
                publicKey[bit / 8] ^= (byte)(1 << (bit % 8));
                Assert.False(ED25519.Verify(signature, message, publicKey));
                publicKey[bit / 8] ^= (byte)(1 << (bit % 8));
            }
            message[0] ^= 1;
            Assert.False(ED25519.Verify(signature, message, publicKey));
            message[0] ^= 1;
            var order = (BigInteger.One << 252) + BigInteger.Parse("27742317777372353535851937790883648493");
            var scalar = new BigInteger(signature.AsSpan(32), true);
            (scalar + order).ToByteArray(true).CopyTo(signature, 32);
            Assert.False(ED25519.Verify(signature, message, publicKey));
            signature.AsSpan(32).Clear();
            order.ToByteArray(true).CopyTo(signature, 32);
            Assert.False(ED25519.Verify(signature, message, publicKey));
        }

        [Fact]
        public void RejectsIdentityForgeryAndMalformedPoints()
        {
            var message = new byte[] { 42 };
            var signature = new byte[64];
            // With A = identity, R = B and S = 1 satisfy the verification equation for every message.
            signature[0] = 0x58;
            signature.AsSpan(1, 31).Fill(0x66);
            signature[32] = 1;
            var identity = new byte[32];
            identity[0] = 1;
            Assert.False(ED25519.Verify(signature, message, identity));
            var prime = (BigInteger.One << 255) - 19;
            foreach (var y in new[] { BigInteger.Zero, BigInteger.One, prime - 1, prime, prime + 1 })
            {
                for (var sign = 0; sign <= 1; sign++)
                {
                    var point = new byte[32];
                    y.ToByteArray(true).CopyTo(point, 0);
                    point[31] |= (byte)(sign << 7);
                    Assert.False(ED25519.Verify(signature, message, point));
                    var malformedR = ED25519.Sign(message, new byte[32]);
                    point.CopyTo(malformedR, 0);
                    Assert.False(ED25519.Verify(malformedR, message, ED25519.GetPublicKey(new byte[32])));
                }
            }
        }

        [Fact]
        public void SupportsOverlappingBuffers()
        {
            var seed = new byte[32];
            RandomNumberGenerator.Fill(seed);
            var publicKey = ED25519.GetPublicKey(seed);
            var message = new byte[128];
            RandomNumberGenerator.Fill(message);
            var expected = ED25519.Sign(message, seed);
            ED25519.Sign(message, seed, message.AsSpan(17, 64));
            Assert.Equal(expected, message.AsSpan(17, 64).ToArray());
            var buffer = new byte[64];
            seed.CopyTo(buffer, 0);
            expected = ED25519.Sign(ReadOnlySpan<byte>.Empty, seed);
            ED25519.Sign(ReadOnlySpan<byte>.Empty, buffer.AsSpan(0, 32), buffer);
            Assert.Equal(expected, buffer);
            ED25519.GetPublicKey(seed, seed);
            Assert.Equal(publicKey, seed);
        }

        [Fact]
        public void ValidatesLengthsBeforeWriting()
        {
            var seed = new byte[32];
            var output = new byte[64];
            Array.Fill(output, (byte)0xcc);
            Assert.Throws<ArgumentException>(() => ED25519.Sign([], new byte[31], output));
            Assert.All(output, value => Assert.Equal(0xcc, value));
            foreach (var length in new[] { 0, 31, 33, 64 })
            {
                Assert.Throws<ArgumentException>(() => ED25519.GetPublicKey(new byte[length]));
                Assert.Throws<ArgumentException>(() => ED25519.GetPublicKey(seed, new byte[length]));
            }
            foreach (var length in new[] { 0, 32, 63, 65 })
            {
                Assert.Throws<ArgumentException>(() => ED25519.Sign([], seed, new byte[length]));
                Assert.False(ED25519.Verify(new byte[length], [], seed));
            }
            Assert.False(ED25519.Verify(output, [], new byte[31]));
        }

        [Fact]
        public void ConcurrentCallsDoNotShareMutableScratchBuffers()
        {
            System.Threading.Tasks.Parallel.For(0, 64, i =>
            {
                var seed = new byte[32];
                seed[0] = (byte)i;
                var message = new byte[] { (byte)i };
                var signature = ED25519.Sign(message, seed);
                Assert.True(ED25519.Verify(signature, message, ED25519.GetPublicKey(seed)));
            });
        }
    }
}

