// =================================================={By Qcbf|qcbf@qq.com|12/15/2024 3:51:09 PM}==================================================

using FLib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable SuspiciousTypeConversion.Global
#pragma warning disable CA2211

namespace FLib
{
    public static class ConfigBuilder
    {
        public static int ConfigTableCompressSize = 1024;
        public static char Sign = '*';
        public static string OutputPath = "cfg.bytes";
        public static Action<ConcurrentDictionary<Type, IConfigBuildTableContext>, Dictionary<string, List<SourceFileMeta>>> CustomBuilder;

        private static readonly Func<IEnumerable<Type>> GetAllTypes = () =>
            TypeAssistant.AllAssemblies
                .Where(asm =>
                {
                    if (asm == typeof(ConfigHelper).Assembly)
                        return false;
                    var asmName = asm.GetName().Name;
                    return !asmName.StartsWith("FLib", StringComparison.Ordinal) && !asmName.EndsWith("Editor", StringComparison.Ordinal) &&
                           !asmName.StartsWith("UnityEngine", StringComparison.Ordinal);
                }).SelectMany(v => v.ExportedTypes);

        public static readonly Func<IReadOnlyDictionary<string, IBuildable>> GetConfigBuilders = () =>
            TypeAssistant.AllAssemblies.Append(typeof(ConfigBuilder).Assembly).SelectMany(v => v.ExportedTypes).Where(t => !t.IsInterface && typeof(IBuildable).IsAssignableFrom(t))
                .Select(t => (IBuildable)TypeAssistant.New(t)).ToDictionary(k => k.Extension);

        /// <summary>
        /// 
        /// </summary>
        public interface IBuildable
        {
            void Build(in TableContext ctx);
            string Extension { get; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class EmptyBuilder : IBuildable
        {
            public static readonly EmptyBuilder Default = new();
            public string Extension => "";

            public void Build(in TableContext ctx)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class TableContext : IConfigBuildTableContext
        {
            private SpinLock _locker;
            private uint _dynamicIdIncrement = 1;
            private readonly TypeCode _indexIdTypeCode;
            public SourceFileMeta SourceFile;
            public Type ConfigType { get; set; }
            public FieldInfo IndexIdField { get; }
            public ConfigHelper.EOption Options { get; set; }
            public List<(uint Id, IBytesPackable Cfg)> AllConfigs { get; set; } = new(128);
            public Dictionary<uint, int> AllConfigIdIndexes { get; set; } = new(128);
            public string SourceFilePath => SourceFile.FilePath;
            public string ConfigName => SourceFile.ConfigName;
            public List<string> ConfigNameArgs => SourceFile.Args;
            public override string ToString() => $"[{ConfigType.Name}]{SourceFile}";

            public TableContext(SourceFileMeta sourceFile, Type type, ConfigHelper.EOption options)
            {
                SourceFile = sourceFile;
                ConfigType = type;
                Options = options;
                IndexIdField = ConfigType.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(v => v.MetadataToken).FirstOrDefault();
                _indexIdTypeCode = Type.GetTypeCode(IndexIdField?.FieldType);
            }

            /// <summary>
            /// 
            /// </summary>
            public void EnsureCapacity(int capacity)
            {
#if NET6_0_OR_GREATER
                AllConfigs.EnsureCapacity(capacity);
#else
                if (capacity > AllConfigs.Capacity)
                    AllConfigs.Capacity = capacity;
#endif
                AllConfigIdIndexes.EnsureCapacity(capacity);
            }

            public (uint Id, int Index)? AddConfigByDynamicId(IBytesPackable config)
            {
                var isLocking = false;
                _locker.Enter(ref isLocking);
                try
                {
                    while (AllConfigIdIndexes.ContainsKey(_dynamicIdIncrement))
                        ++_dynamicIdIncrement;
                    var index = AllConfigs.Count;
                    AllConfigs.Add((_dynamicIdIncrement, config));
                    AllConfigIdIndexes.Add(_dynamicIdIncrement, index);
                    return (_dynamicIdIncrement, index);
                }
                finally
                {
                    if (isLocking)
                        _locker.Exit(false);
                }
            }

            /// <summary>
            /// 
            /// </summary>0
            public (uint Id, int Index)? AddConfig(object objId, IBytesPackable config, TypeCode overrideTypeCode = TypeCode.Empty)
            {
                if (objId == null || config == null)
                    return null;
                var isLocking = false;
                _locker.Enter(ref isLocking);
                try
                {
                    var index = AllConfigs.Count;
                    if (overrideTypeCode == TypeCode.Empty)
                        overrideTypeCode = _indexIdTypeCode;
                    var id = overrideTypeCode >= TypeCode.SByte && overrideTypeCode <= TypeCode.UInt64 ? Convert.ToUInt32(objId) : ConfigHelper.StringToUniqueId(objId.ToString());
                    if (!AllConfigIdIndexes.TryAdd(id, index))
                        Log.Error?.Write($"存在相同Id配置: {ConfigType.Name}.{objId}\n{SourceFile}");
                    AllConfigs.Add((id, config));
                    return (id, index);
                }
                finally
                {
                    if (isLocking)
                        _locker.Exit();
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public class SourceFileMeta
        {
            public IBuildable Builder;
            public string FilePath;
            public string ConfigName;
            public char FileSign;
            public List<string> Args;
            public override string ToString() => FilePath;
        }

        /// <summary>
        /// 
        /// </summary>
        public static int Build(string sourceDirPath)
        {
            try
            {
                ConfigPostBuildProcessData.AdditionConfigPostBuildProcesses = new List<ConfigPostBuildProcessData>();
                var tableContexts = BuildTables(sourceDirPath);
                PostBuildProcess(tableContexts);
                ConfigPostBuildProcessData.AdditionConfigPostBuildProcesses = null;
                var outPath = Path.GetFullPath(OutputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                var bytes = GenerateConfigBytes(tableContexts);
                File.WriteAllBytes(outPath, bytes.Span.ToArray());
                return tableContexts.Count;
            }
            catch (Exception ex)
            {
                Log.Error?.Write(ex.ToString());
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        private static IReadOnlyDictionary<Type, IConfigBuildTableContext> BuildTables(string sourceDirPath)
        {
            var sourceFileMetas = new Dictionary<string, List<SourceFileMeta>>();
            var allConfigBuilders = GetConfigBuilders();
            var strbuf = new StringBuilder();
            foreach (var filePath in Directory.GetFiles(sourceDirPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (filePath.EndsWith(".meta", StringComparison.Ordinal))
                        continue;
                    var name = Path.GetFileNameWithoutExtension(filePath);
                    if (name.StartsWith('~') || name.StartsWith('_') || name.EndsWith('~') || name.EndsWith(".schema", StringComparison.Ordinal) || name.EndsWith(".ai", StringComparison.Ordinal))
                        continue;
                    if (!allConfigBuilders.TryGetValue(Path.GetExtension(filePath), out var builder))
                        continue;
                    var meta = new SourceFileMeta() { FileSign = '*', FilePath = filePath, Builder = builder };
                    var argStartIndex = 0;
                    for (var i = 0; i < name.Length; i++)
                    {
                        switch (name[i])
                        {
                            case '$':
                                meta.FileSign = name[++i]; // step $
                                break;
                            case '.' when argStartIndex == 0:
                                argStartIndex = i + 1;
                                break;
                            case '.':
                                (meta.Args ??= new List<string>()).Add(name[argStartIndex..]);
                                argStartIndex = 0;
                                break;
                            default:
                            {
                                if (argStartIndex == 0)
                                {
                                    strbuf.Append(name[i]);
                                }

                                break;
                            }
                        }
                    }

                    if (argStartIndex > 0)
                        (meta.Args ??= new List<string>()).Add(name[argStartIndex..]);

                    meta.ConfigName = strbuf.ToString();
                    strbuf.Clear();

                    if (!sourceFileMetas.TryGetValue(meta.ConfigName, out var metaList))
                        sourceFileMetas.Add(meta.ConfigName, new List<SourceFileMeta> { meta });
                    else
                        metaList.Add(meta);
                }
                catch (Exception e)
                {
                    throw new Exception($"{filePath}", e);
                }
            }

            var contexts = new ConcurrentDictionary<Type, IConfigBuildTableContext>(Environment.ProcessorCount, 1024);
            CustomBuilder?.Invoke(contexts, sourceFileMetas);
            GetAllTypes().AsParallel().ForAll(t => BuildTablesAddContext(t, contexts, sourceFileMetas));
            return contexts;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void BuildTablesAddContext(Type type, ConcurrentDictionary<Type, IConfigBuildTableContext> contexts, Dictionary<string, List<SourceFileMeta>> allFileMetas)
        {
            var attr = type.GetCustomAttribute<ConfigAttribute>();
            if (attr?.ConfigFileName == null)
                return;
            if (!allFileMetas.TryGetValue(attr.ConfigFileName, out var fileMetas))
            {
                Log.Info?.Write($"not found config file {attr.ConfigFileName}");
                return;
            }

            foreach (var fileMeta in fileMetas)
            {
                if (!ConfigBuilderUtility.CheckSign(fileMeta.FileSign, Sign))
                    continue;
                var ctx = (TableContext)contexts.GetOrAdd(type, new TableContext(fileMeta, type, attr.Options));
                try
                {
                    var configType = ctx.ConfigType;
                    if (typeof(IConfigFileCustomBuildToTable).IsAssignableFrom(configType))
                    {
                        var deserializer = (IConfigFileCustomBuildToTable)TypeAssistant.New(configType);
                        deserializer.ConfigFileDeserializeToTable(Sign, ctx, contexts);
                    }
                    else
                    {
                        if (fileMeta.Builder == null)
                            Log.Error?.Write($"not found config builder {type} {attr.ConfigFileName} {fileMeta.FilePath}");
                        else
                            fileMeta.Builder.Build(ctx);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error?.Write($"{ctx}\n{ex}");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void PostBuildProcess(IReadOnlyDictionary<Type, IConfigBuildTableContext> allContexts)
        {
            foreach (var item in ConfigPostBuildProcessData.AdditionConfigPostBuildProcesses)
            {
                if (item.CfgType == null)
                    item.Process.OnConfigPostBuildProcess(Sign, null, allContexts);
                else if (allContexts.TryGetValue(item.CfgType, out var ctx))
                    item.Process.OnConfigPostBuildProcess(Sign, ctx, allContexts);
                else
                    throw new Exception($"not found config {item.CfgType}");
            }

            allContexts.AsParallel().ForAll(item =>
            {
                var ctx = item.Value;
                if (typeof(IConfigPostBuildProcessable).IsAssignableFrom(ctx.ConfigType))
                {
                    try
                    {
                        if (ctx.AllConfigs.Count > 2048)
                        {
                            ctx.AllConfigs.AsParallel().ForAll(cfgLine => ProcessConfigPostBuild(cfgLine.Id, cfgLine.Cfg, ctx, allContexts));
                        }
                        else
                        {
                            foreach (var (id, cfg) in ctx.AllConfigs)
                                ProcessConfigPostBuild(id, cfg, ctx, allContexts);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error?.Write($"{ctx}\n{ex}");
                    }
                }
            });
            return;

            static void ProcessConfigPostBuild(uint id, IBytesPackable cfg, IConfigBuildTableContext ctx, IReadOnlyDictionary<Type, IConfigBuildTableContext> allContexts)
            {
                try
                {
                    ((IConfigPostBuildProcessable)cfg).OnConfigPostBuildProcess(Sign, ctx, allContexts);
                }
                catch (Exception ex)
                {
                    Log.Error?.Write($"{id}.{ctx}\n{ex}");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static BytesWriter GenerateConfigBytes(IReadOnlyDictionary<Type, IConfigBuildTableContext> contexts)
        {
            var writer = new BytesWriter();
            writer.Allocate(contexts.Count * 1024);
            writer.PushLength(contexts.Count);

            var packBuffer = new BytesWriter();
            foreach (var ctx in contexts.OrderBy(v => v.Key.MetadataToken))
            {
                var options = ctx.Value.Options;
                writer.Push(TypeAssistant.GetTypeName(ctx.Key), Encoding.ASCII);
                writer.PushLength(ctx.Value.AllConfigs.Count);
                IEnumerable<(uint, IBytesPackable)> configs = ctx.Value.AllConfigs;
                if ((ctx.Value.Options & ConfigHelper.EOption.OrderById) != 0)
                    configs = configs.OrderBy(v => v.Item1);
                foreach (var (id, cfg) in configs)
                {
                    packBuffer.Clear();
                    BytesPack.Pack(cfg, ref packBuffer);
                    var copyOptions = options;
                    if (packBuffer.Length >= ConfigTableCompressSize)
                        copyOptions |= ConfigHelper.EOption.AlwaysCompressRawData;
                    if ((copyOptions & ConfigHelper.EOption.AlwaysCompressRawData) != 0)
                        packBuffer = new BytesWriter(Compressor.Compress(packBuffer));
                    writer.PushVInt(id);
                    writer.Push(copyOptions);
                    writer.Push(packBuffer.Span);
                }
            }

            return writer;
        }
    }
}