// ==================={By Qcbf|qcbf@qq.com|2/24/2022 4:52:35 PM}===================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

// ReSharper disable StaticMemberInGenericType
#pragma warning disable CA2211
// ReSharper disable MemberHidesStaticFromOuterClass

namespace FLib
{
    /// <summary>
    /// 配置表运行时访问入口。
    /// </summary>
    public static class Config<T> where T : IBytesPackable, new()
    {
        /// <summary> 未找到配置时返回的默认值。 </summary>
        public static readonly T DefaultConfig = default;
        /// <summary> 所有配置元数据，按表内顺序存储。 </summary>
        public static Meta[] AllMetas;
        /// <summary> 配置 ID 到表内索引的映射。 </summary>
        public static IReadOnlyDictionary<uint, int> IdMetas;
        /// <summary> 当前配置数量。 </summary>
        public static int Count => (AllMetas?.Length).GetValueOrDefault();

        /// <summary>
        /// 单条配置的缓存状态。
        /// </summary>
        public struct Meta
        {
            /// <summary> 原始序列化数据，可能为压缩数据。 </summary>
            public byte[] RawBytes;
            /// <summary> 反序列化后的配置值。 </summary>
            public T Value;
            /// <summary> 当前存储和加载选项。 </summary>
            public ConfigHelper.EOption Options;
        }

        /// <summary>
        /// 配置遍历入口。
        /// </summary>
        public readonly struct Enumerable : IEnumerable<T>
        {
            public Enumerator GetEnumerator() => Config<T>.GetEnumerator();
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// 配置遍历器。
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            /// <summary> 当前表内索引。 </summary>
            public int Index;

            /// <summary> 当前配置的引用，会按需解压和反序列化。 </summary>
            public readonly ref T CurrentRef
            {
                get
                {
                    ref var meta = ref AllMetas[Index];
                    TryDecompressRawBytes(ref meta, true);
                    return ref meta.Value;
                }
            }

            public readonly T Current => CurrentRef;
            readonly object IEnumerator.Current => Current;
            public bool MoveNext() => ++Index < Count;
            public void Reset() => Index = -1;

            public readonly void Dispose()
            {
            }
        }

        /// <summary>
        /// 判断字符串 ID 对应的配置是否存在。
        /// </summary>
        public static bool Contains(string id)
        {
            return Contains(ConfigHelper.StringToUniqueId(id));
        }

        /// <summary>
        /// 判断数值 ID 对应的配置是否存在。
        /// </summary>
        public static bool Contains(uint id)
        {
            return IdMetas.ContainsKey(id);
        }

        /// <summary>
        /// 按字符串 ID 获取配置引用；未找到时返回默认值。
        /// </summary>
        public static ref readonly T Get(string id, ELogLevel logLevel = ELogLevel.Fatal)
        {
            var index = GetIndex(id, logLevel);
            if (index < 0)
                return ref DefaultConfig;
            return ref Index(index);
        }

        /// <summary>
        /// 按数值 ID 获取配置引用；未找到时返回默认值。
        /// </summary>
        public static ref readonly T Get(uint id, ELogLevel logLevel = ELogLevel.Fatal)
        {
            var index = GetIndex(id, logLevel);
            if (index < 0)
                return ref DefaultConfig;
            return ref Index(index);
        }

        /// <summary>
        /// 按数值 ID 获取表内索引；未找到返回 -1。
        /// </summary>
        public static int GetIndex(uint id, ELogLevel logLevel = ELogLevel.Fatal)
        {
            if (IdMetas.TryGetValue(id, out var index))
                return index;
            Log.Get(logLevel)?.Write($"{typeof(T).Name} not found id: {id}");
            return -1;
        }

        /// <summary>
        /// 按字符串 ID 获取表内索引；未找到返回 -1。
        /// </summary>
        public static int GetIndex(string id, ELogLevel logLevel = ELogLevel.Fatal)
        {
            if (IdMetas.TryGetValue(ConfigHelper.StringToUniqueId(id), out var index))
                return index;
            Log.Get(logLevel)?.Write($"{typeof(T).Name} not found id: {id}");
            return -1;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 按表内索引获取配置引用；索引无效时返回默认值。
        /// </summary>
        public static ref readonly T Index(int configMetaIndex, ELogLevel logLevel = ELogLevel.Fatal)
        {
            if (AllMetas == null || configMetaIndex < 0 || configMetaIndex >= AllMetas.Length)
            {
                Log.Get(logLevel)?.Write($"not found config index {typeof(T).Name}.{configMetaIndex}");
                return ref DefaultConfig;
            }

            ref var v = ref AllMetas[configMetaIndex];
            TryDecompressRawBytes(ref v);
            TryDeserializeData(ref v);
            return ref v.Value;
        }

        /// <summary>
        /// 将指定 ID 的配置反序列化到目标实例。
        /// </summary>
        public static void CopyTo(uint id, ref T to)
        {
            ref var meta = ref AllMetas[IdMetas[id]];
            TryDecompressRawBytes(ref meta);
            BytesPack.Unpack(ref to, meta.RawBytes);
        }

        /// <summary>
        /// 所有配置的可遍历视图。
        /// </summary>
        public static Enumerable All => default;

        /// <summary>
        /// 创建配置遍历器。
        /// </summary>
        public static Enumerator GetEnumerator()
        {
            return new Enumerator { Index = -1 };
        }

        /// <summary>
        /// 设置或追加一条配置。
        /// </summary>
        public static void Set(uint id, in T v, bool isAdd = false)
        {
            var meta = new Meta { Value = v, Options = ConfigHelper.EOption.__DeserializedData };
            if (IdMetas.TryGetValue(id, out var index))
            {
                AllMetas[index] = meta;
            }
            else if (isAdd)
            {
                index = AllMetas.Length;
                Array.Resize(ref AllMetas, index + 1);
                AllMetas[index] = meta;
                var newMetas = IdMetas.ToList();
                newMetas.Add(new KeyValuePair<uint, int>(id, index));
                IdMetas = new ReadOnlyDictionary<uint, int>(newMetas.ToDictionary(k => k.Key, v => v.Value));
            }
            else
            {
                throw new Exception($"not found Id: {typeof(T).Name}.{id}");
            }
        }

        /// <summary>
        /// 从字节流解析单条配置元数据。
        /// </summary>
        public static void DeserializeConfigParse(uint id, ref BytesReader reader, out Meta meta, bool isSkip = false, ConfigHelper.EOption addOptions = default)
        {
            meta = new Meta { Options = reader.Read<ConfigHelper.EOption>() | addOptions };
            if (isSkip)
            {
                var len = reader.ReadLength();
                reader.Position += len;
            }
            else
            {
                meta.RawBytes = reader.ReadArray<byte>();
            }

            try
            {
                if (!isSkip && (meta.Options & ConfigHelper.EOption.AlwaysDeserializeData) != 0)
                {
                    meta.Value = new T();
                    BytesPack.Unpack(ref meta.Value, meta.RawBytes, $"{typeof(T).Name}->{id}");
                    meta.Options = ConfigHelper.EOption.__DeserializedData;
                    if ((meta.Options & ConfigHelper.EOption.AlwaysStoreRawBytes) == 0)
                        meta.RawBytes = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error?.Write($"deserialize config error: {typeof(T).Name}->{id}\n{ex}");
            }
        }

        private static void TryDeserializeData(ref Meta meta)
        {
            if ((meta.Options & ConfigHelper.EOption.__DeserializedData) != 0) return;
            meta.Options |= ConfigHelper.EOption.__DeserializedData;
            meta.Value = new T();
            BytesPack.Unpack(ref meta.Value, meta.RawBytes);
            if ((meta.Options & ConfigHelper.EOption.AlwaysStoreRawBytes) == 0)
                meta.RawBytes = null;
        }

        private static void TryDecompressRawBytes(ref Meta meta, bool isWithDeserialize = false)
        {
            if ((meta.Options & ConfigHelper.EOption.AlwaysCompressRawData) != 0)
            {
                meta.RawBytes = Compressor.Uncompress(meta.RawBytes).ToArray();
                meta.Options &= ~ConfigHelper.EOption.AlwaysCompressRawData;
            }

            if (isWithDeserialize)
                TryDeserializeData(ref meta);
        }

        internal static int DeserializeConfigTable(in Memory<byte> buffer)
        {
            BytesReader reader = buffer;
            var count = reader.ReadLength();
            var idMetas = new Dictionary<uint, int>();
            AllMetas = new Meta[count];
            for (var i = 0; i < count; i++)
            {
                var id = (uint)reader.ReadVInt();
                DeserializeConfigParse(id, ref reader, out var meta);
                if (!idMetas.TryAdd(id, i))
                    throw new Exception($"found already exist sid: {typeof(T).Name}.{id}");
                AllMetas[i] = meta;
            }

            IdMetas =
#if NET6_0_OR_GREATER
                System.Collections.Immutable.ImmutableDictionary.ToImmutableDictionary(idMetas);
#else
                new ReadOnlyDictionary<uint, int>(idMetas);
#endif
            return reader.Position;
        }
    }

    /// <summary>
    /// 配置表通用工具。
    /// </summary>
    public static class ConfigHelper
    {
        [Flags]
        public enum EOption : byte
        {
            /// <summary>
            /// 总是保留字节数据，而不是反序列之后释放 （该选项会增加内存占用）
            /// </summary>
            AlwaysStoreRawBytes = 0x1,

            /// <summary>
            /// 总是反序列化数据，而不是等待使用时反序列化 （该选项会增加内存占用和性能峰值过高）
            /// </summary>
            AlwaysDeserializeData = 0x2,

            /// <summary>
            /// 总是压缩，默认是根据数据大小自动压缩 (该选项会增加性能开销，减少内存占用)
            /// </summary>
            AlwaysCompressRawData = 0x4,

            /// <summary>
            /// 按照id排序
            /// </summary>
            OrderById = 0x8,

            /// <summary>
            /// 已经反序列过数据（主要运行时内部使用的标识）
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Advanced)]
            // ReSharper disable once InconsistentNaming
            __DeserializedData = 0x80,
        }

        /// <summary>
        /// 将字符串 ID 转为配置表使用的数值 ID。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint StringToUniqueId(string str)
        {
            return (uint)StringFLibUtility.ShortStringToHash(str);
        }

        /// <summary>
        /// 从文件加载全部配置表。
        /// </summary>
        public static int DeserializeAll(string path, out int buildConfigCount) => DeserializeAll(File.ReadAllBytes(path), out buildConfigCount);

        /// <summary>
        /// 从内存数据加载全部配置表。
        /// </summary>
        public static int DeserializeAll(Memory<byte> buffer, out int buildConfigCount)
        {
            buffer = Compressor.Uncompress(buffer.Span).ToArray();
            BytesReader reader = buffer;
            buildConfigCount = reader.ReadLength();
            var deserializeConfigTableParams = new object[1];
            var initializers = new List<MethodInfo>();
            for (var i = 0; i < buildConfigCount; i++)
            {
                var configType = TypeAssistant.GetType(reader.ReadString(Encoding.ASCII));
                var customDeserialize = configType.GetMethod("CustomDeserialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                deserializeConfigTableParams[0] = buffer[reader.Position..];
                if (customDeserialize != null)
                {
                    customDeserialize.Invoke(null, deserializeConfigTableParams);
                }
                else
                {
                    var configTypeWrap = typeof(Config<>).MakeGenericType(configType);
                    reader.Position += (int)configTypeWrap.GetMethod("DeserializeConfigTable", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, deserializeConfigTableParams)!;
                }

                var initializer = configType.GetMethod("OnAllConfigInitialized", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (initializer != null)
                    initializers.Add(initializer);
            }

            foreach (var initializer in initializers)
                initializer.Invoke(null, null);
            return reader.Position;
        }

        /// <summary> 将指定 ID 的配置复制到目标实例。 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(uint id, ref T to) where T : IBytesPackable, new()
        {
            Config<T>.CopyTo(id, ref to);
        }
    }

    /// <summary>
    /// 标记类型对应的配置表文件。
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class ConfigAttribute : CommentAttribute
    {
        /// <summary> 配置文件名。 </summary>
        public string ConfigFileName => Name;
        /// <summary> 配置加载选项。 </summary>
        public ConfigHelper.EOption Options;

        /// <summary> 指定配置文件名。 </summary>
        public ConfigAttribute(string configFileName) : base(configFileName)
        {
        }

        /// <summary> 指定配置文件名和加载选项。 </summary>
        public ConfigAttribute(string configFileName, ConfigHelper.EOption options) : this(configFileName) => Options = options;
    }

    /// <summary>
    /// 当配置表build之后的回调
    /// </summary>
    public interface IConfigPostBuildProcessable
    {
        void OnConfigPostBuildProcess(char sign, IConfigBuildTableContext context, IReadOnlyDictionary<Type, IConfigBuildTableContext> allTableContexts);
    }

    /// <summary>
    /// 当配置表build之后的回调, 自己注册
    /// </summary>
    public class ConfigPostBuildProcessData
    {
        /// <summary> 额外注册的构建后处理。 </summary>
        public static List<ConfigPostBuildProcessData> AdditionConfigPostBuildProcesses;
        /// <summary> 目标配置类型。 </summary>
        public Type CfgType;
        /// <summary> 构建后处理实例。 </summary>
        public IConfigPostBuildProcessable Process;
    }

    /// <summary>
    /// 配置文件自定义构建到表, 由自己写入到context
    /// </summary>
    public interface IConfigFileCustomBuildToTable
    {
        void ConfigFileDeserializeToTable(char sign, IConfigBuildTableContext context, IReadOnlyDictionary<Type, IConfigBuildTableContext> allTableContexts);
    }

    /// <summary>
    /// 配置表构建上下文
    /// </summary>
    public interface IConfigBuildTableContext
    {
        Type ConfigType { get; set; }
        FieldInfo IndexIdField { get; }
        string SourceFilePath { get; }
        string ConfigName { get; }
        List<string> ConfigNameArgs { get; }
        ConfigHelper.EOption Options { get; set; }
        List<(uint Id, IBytesPackable Cfg)> AllConfigs { get; set; }
        Dictionary<uint, int> AllConfigIdIndexes { get; set; }
        (uint Id, int Index)? AddConfig(object objId, IBytesPackable config);
        (uint Id, int Index)? AddConfigByDynamicId(IBytesPackable config);
    }
}
