//==================={By Qcbf|qcbf@qq.com|12/28/2021 5:48:08 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FLib.Unity
{
    [BytesPackGen]
    public partial struct AssetLoaderInfo
    {
        [BytesPackGenField] public uint Id;
        [BytesPackGenField] public SlimDictionary<string, Meta> AssetMetas;
        public bool IsEmpty => Id == 0;

        [BytesPackGen]
        public partial struct Meta : IEquatable<Meta>
        {
            [BytesPackGenField] public Hash128 Hash;
            [BytesPackGenField] public int Size;
            [BytesPackGenField] public string[] Dependencies;
            private string _fileNameStr;
            public string FileNameStr => _fileNameStr ??= Hash + AssetLoader.BUNDLE_EXTENSION;

            public readonly override string ToString()
            {
                var strbuf = new StringBuilder();
                strbuf.Append(FIO.FormatSize(Size)).Append(',');
                strbuf.Append(Hash).Append(',');
                if (Dependencies != null)
                {
                    strbuf.Append("Dependencies[").Append(Dependencies.Length).AppendLine("]");
                    foreach (var item in Dependencies) strbuf.Append(' ', 2).Append('"').Append(item).Append('"').AppendLine();
                }
                return strbuf.ToString();
            }

            public readonly override bool Equals(object obj) => obj is Meta meta && meta == this;
            public bool Equals(Meta other) => this == other;
            public readonly override int GetHashCode() => HashCode.Combine(Hash, Size, Dependencies);
            public static bool operator !=(in Meta a, in Meta b) => !(a == b);

            public static bool operator ==(in Meta a, in Meta b)
            {
                if (a.Size != b.Size || a.Hash != b.Hash || (a.Dependencies?.Length).GetValueOrDefault() != (b.Dependencies?.Length).GetValueOrDefault())
                    return false;

                for (var i = 0; i < a.Dependencies?.Length; i++)
                {
                    if (b.Dependencies != null && a.Dependencies[i] != b.Dependencies[i])
                        return false;
                }

                return true;
            }
        }

        public AssetLoaderInfo(int capacity) : this(GuidHelper.BaseDate.GetTimestamp(), capacity) { }

        public AssetLoaderInfo(uint id, int capacity)
        {
            Id = id;
            AssetMetas = new SlimDictionary<string, Meta>(capacity);
        }

        public static AssetLoaderInfo Unpack(Span<byte> bytes)
        {
            var info = new AssetLoaderInfo();
            BytesPack.Unpack(ref info, Compressor.Uncompress(bytes));
            return info;
        }

        public readonly override string ToString()
        {
            return $"[{Id}]Assets:{AssetMetas?.Count}";
        }

        public string GetLog()
        {
#if DEBUG
            var strbuf = new StringBuilder(ToString()).AppendLine();
            foreach (var meta in AssetMetas.OrderBy(v => v.Value.Size))
                strbuf.Append(' ').Append("[\"").Append(meta.Key).Append("\"]").AppendLine().Append(' ', 3).AppendLine(meta.Value.ToString());
            return strbuf.ToString();
#else
            return ToString();
#endif
        }
    }
}
