// // ==================== qcbf@qq.com | 2026-09-15 ====================

using System;
using System.Security.Cryptography;

namespace FLib
{
    /// <summary>RFC 8032 Ed25519 (without context or prehash). Private keys are 32-byte seeds.</summary>
    public static partial class ED25519
    {
        public const int PrivateKeySize = 32;
        public const int PublicKeySize = 32;
        public const int SignatureSize = 64;

        /// <summary>Generates a cryptographically random 32-byte private seed.</summary>
        public static byte[] GeneratePrivateKey()
        {
            var privateKey = new byte[PrivateKeySize];
            RandomNumberGenerator.Fill(privateKey);
            return privateKey;
        }

        public static void CreateKeyPair(out byte[] publicKey, out byte[] privateKey)
        {
            privateKey = GeneratePrivateKey();
            publicKey = GetPublicKey(privateKey);
        }

        public static byte[] GetPublicKey(ReadOnlySpan<byte> privateKey)
        {
            var publicKey = new byte[PublicKeySize];
            GetPublicKey(privateKey, publicKey);
            return publicKey;
        }

        /// <summary>Derives a public key from a 32-byte private seed. Output must be exactly 32 bytes.</summary>
        public static void GetPublicKey(ReadOnlySpan<byte> privateKey, Span<byte> publicKey)
        {
            CheckLength(privateKey.Length, PrivateKeySize, nameof(privateKey));
            CheckLength(publicKey.Length, PublicKeySize, nameof(publicKey));
            Span<byte> expandedKey = stackalloc byte[64];
            try
            {
                ExpandPrivateKey(privateKey, expandedKey);
                DerivePublicKey(expandedKey, publicKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expandedKey);
            }
        }

        public static byte[] Sign(ReadOnlySpan<byte> message, ReadOnlySpan<byte> privateKey)
        {
            var signature = new byte[SignatureSize];
            Sign(message, privateKey, signature);
            return signature;
        }

        /// <summary>Signs deterministically using a 32-byte private seed. Output must be exactly 64 bytes; input/output overlap is supported.</summary>
        public static void Sign(ReadOnlySpan<byte> message, ReadOnlySpan<byte> privateKey, Span<byte> signature)
        {
            CheckLength(privateKey.Length, PrivateKeySize, nameof(privateKey));
            CheckLength(signature.Length, SignatureSize, nameof(signature));
            Span<byte> expandedKey = stackalloc byte[64];
            Span<byte> nonce = stackalloc byte[64];
            Span<byte> challenge = stackalloc byte[64];
            Span<byte> publicKey = stackalloc byte[32];
            Span<byte> result = stackalloc byte[64];
            try
            {
                ExpandPrivateKey(privateKey, expandedKey);
                DerivePublicKey(expandedKey, publicKey);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
                hash.AppendData(expandedKey.Slice(32));
                hash.AppendData(message);
                FinishHash(hash, nonce);
                ScalarOperations.sc_reduce(nonce);
                GroupOperations.ge_scalarmult_base(out var r, nonce, 0);
                GroupOperations.ge_p3_tobytes(result, 0, ref r);
                hash.AppendData(result.Slice(0, 32));
                hash.AppendData(publicKey);
                hash.AppendData(message);
                FinishHash(hash, challenge);
                ScalarOperations.sc_reduce(challenge);
                ScalarOperations.sc_muladd(result.Slice(32), challenge, expandedKey, nonce);
                result.CopyTo(signature);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expandedKey);
                CryptographicOperations.ZeroMemory(nonce);
                CryptographicOperations.ZeroMemory(challenge);
                CryptographicOperations.ZeroMemory(result);
            }
        }

        /// <summary>
        /// Verifies a detached signature. Returns false for malformed lengths, noncanonical S/R/public key,
        /// invalid curve points and small-order R/public key. Uses the uncofactored verification equation.
        /// </summary>
        public static bool Verify(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> publicKey)
        {
            if (signature.Length != SignatureSize || publicKey.Length != PublicKeySize ||
                !IsCanonicalScalar(signature.Slice(32)) ||
                !TryDecodePoint(publicKey, out var a) || !TryDecodePoint(signature.Slice(0, 32), out _))
                return false;
            Span<byte> challenge = stackalloc byte[64];
            Span<byte> expectedR = stackalloc byte[32];
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
            hash.AppendData(signature.Slice(0, 32));
            hash.AppendData(publicKey);
            hash.AppendData(message);
            FinishHash(hash, challenge);
            ScalarOperations.sc_reduce(challenge);
            GroupOperations.ge_double_scalarmult_vartime(out var r, challenge, ref a, signature.Slice(32));
            GroupOperations.ge_tobytes(expectedR, 0, ref r);
            return CryptographicOperations.FixedTimeEquals(expectedR, signature.Slice(0, 32));
        }

        #region non-public

        private static void CheckLength(int actual, int expected, string parameter)
        {
            if (actual != expected)
                throw new ArgumentException($"Expected exactly {expected} bytes.", parameter);
        }

        private static void ExpandPrivateKey(ReadOnlySpan<byte> privateKey, Span<byte> expandedKey)
        {
            using var hash = SHA512.Create();
            if (!hash.TryComputeHash(privateKey, expandedKey, out var written) || written != 64)
                throw new CryptographicException("SHA-512 failed.");
            ScalarOperations.sc_clamp(expandedKey, 0);
        }

        private static void DerivePublicKey(ReadOnlySpan<byte> expandedKey, Span<byte> publicKey)
        {
            GroupOperations.ge_scalarmult_base(out var a, expandedKey, 0);
            GroupOperations.ge_p3_tobytes(publicKey, 0, ref a);
        }

        private static void FinishHash(IncrementalHash hash, Span<byte> destination)
        {
            if (!hash.TryGetHashAndReset(destination, out var written) || written != 64)
                throw new CryptographicException("SHA-512 failed.");
        }

        private static bool IsCanonicalScalar(ReadOnlySpan<byte> scalar)
        {
            ReadOnlySpan<byte> order = stackalloc byte[32]
            {
                0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58,
                0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x10
            };
            for (var i = 31; i >= 0; i--)
            {
                if (scalar[i] != order[i])
                    return scalar[i] < order[i];
            }
            return false;
        }

        private static bool TryDecodePoint(ReadOnlySpan<byte> encoded, out GroupElementP3 point)
        {
            if (GroupOperations.ge_frombytes_negate_vartime(out point, encoded, 0) != 0)
                return false;
            Span<byte> canonical = stackalloc byte[32];
            FieldOperations.fe_tobytes(canonical, 0, ref point.Y);
            canonical[31] |= (byte)(encoded[31] & 0x80);
            if (!canonical.SequenceEqual(encoded) ||
                ((encoded[31] & 0x80) != 0 && FieldOperations.fe_isnonzero(ref point.X) == 0))
                return false;

            // Reject all points of order dividing the cofactor (8), including the identity.
            var multiplied = point;
            for (var i = 0; i < 3; i++)
            {
                GroupOperations.ge_p3_dbl(out var doubled, ref multiplied);
                GroupOperations.ge_p1p1_to_p3(out multiplied, ref doubled);
            }
            FieldOperations.fe_sub(out var yMinusZ, ref multiplied.Y, ref multiplied.Z);
            return FieldOperations.fe_isnonzero(ref multiplied.X) != 0 || FieldOperations.fe_isnonzero(ref yMinusZ) != 0;
        }

        #endregion
    }
}
