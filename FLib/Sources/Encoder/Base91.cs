// // ==================== qcbf@qq.com | 2026-09-15 ====================

using System;

namespace FLib
{
    /// <summary>在二进制数据与 Base91 文本之间进行编码和解码。</summary>
    public static class Base91
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%&()*+,./:;<=>?@[]^_`{|}~\"";
        private static readonly sbyte[] DecodeTable = CreateDecodeTable();

        private static sbyte[] CreateDecodeTable()
        {
            var table = new sbyte[256];
            Array.Fill(table, (sbyte)-1);
            for (var i = 0; i < Alphabet.Length; i++) table[Alphabet[i]] = (sbyte)i;
            return table;
        }

        /// <summary>使用 Base91 编码指定的二进制数据。</summary>
        /// <param name="data">要编码的二进制数据。</param>
        /// <returns>Base91 编码后的字符串。</returns>
        public static string Encode(ReadOnlySpan<byte> data)
        {
            var output = new char[(data.Length * 16 + 12) / 13];
            var outputCount = 0;
            ulong bitQueue = 0;
            var bitCount = 0;

            foreach (var b in data)
            {
                bitQueue |= (ulong)b << bitCount;
                bitCount += 8;

                while (bitCount >= 13)
                {
                    var value = bitQueue & 8191;
                    var bits = 13;
                    if (value <= 88)
                    {
                        value = bitQueue & 16383;
                        bits = 14;
                    }

                    bitQueue >>= bits;
                    bitCount -= bits;
                    output[outputCount++] = Alphabet[(int)(value % 91)];
                    output[outputCount++] = Alphabet[(int)(value / 91)];
                }
            }

            if (bitCount <= 0) return new string(output, 0, outputCount);
            output[outputCount++] = Alphabet[(int)(bitQueue % 91)];
            if (bitCount > 7 || bitQueue > 90)
                output[outputCount++] = Alphabet[(int)(bitQueue / 91)];

            return new string(output, 0, outputCount);
        }

        /// <summary>将 Base91 文本解码为原始二进制数据，Base91 字符表之外的字符会被忽略。</summary>
        /// <param name="text">要解码的 Base91 编码文本。</param>
        /// <returns>解码后的二进制数据。</returns>
        public static byte[] Decode(ReadOnlySpan<char> text)
        {
            var result = new byte[text.Length * 14 / 16 + 2];
            var resultCount = 0;
            ulong bitQueue = 0;
            var bitCount = 0;
            var value = -1;

            foreach (var c in text)
            {
                var idx = c <= byte.MaxValue ? DecodeTable[c] : (sbyte)-1;
                if (idx < 0) continue;

                if (value < 0)
                {
                    value = idx;
                    continue;
                }

                value += idx * 91;
                bitQueue |= (ulong)value << bitCount;
                bitCount += (value & 8191) > 88 ? 13 : 14;

                do
                {
                    result[resultCount++] = (byte)bitQueue;
                    bitQueue >>= 8;
                    bitCount -= 8;
                } while (bitCount > 7);

                value = -1;
            }

            if (value >= 0)
                result[resultCount++] = (byte)(bitQueue | (ulong)value << bitCount);

            return resultCount == result.Length ? result : result.AsSpan(0, resultCount).ToArray();
        }
    }
}
