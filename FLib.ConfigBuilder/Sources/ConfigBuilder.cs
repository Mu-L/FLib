// // ==================== qcbf@qq.com | 2026-07-04 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FLib
{
    public static class ConfigBuilder
    {
        public static int ConfigTableCompressSize = 1024;
        public static char Sign = '*';
        public static string OutputPath = "cfg.bytes";
        public static Action<ConcurrentDictionary<Type, IConfigTable>, Dictionary<string, List<IConfigFile>>> CustomBuilder;

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

        public static readonly Func<IReadOnlyDictionary<string, IConfigBuildable>> GetConfigBuilders = () =>
            TypeAssistant.AllAssemblies.Append(typeof(ConfigBuilder).Assembly).SelectMany(v => v.ExportedTypes).Where(t => !t.IsInterface && typeof(IConfigBuildable).IsAssignableFrom(t))
                .Select(t => (IConfigBuildable)TypeAssistant.New(t)).ToDictionary(k => k.Extension);

        /// <summary> 构建配置 </summary>
        public static int Build(params string[] sourceDirectories)
        {
            try
            {
                ConfigPostBuildProcessData.AdditionConfigPostBuildProcesses = new List<ConfigPostBuildProcessData>();
                var allFiles = BuildFiles(sourceDirectories);
                var allTables = BuildTables(allFiles);
                PostBuildProcess(allFiles, allTables);
                ConfigPostBuildProcessData.AdditionConfigPostBuildProcesses = null;

                var outPath = Path.GetFullPath(OutputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                File.WriteAllBytes(outPath, Compressor.Compress(GenerateConfigBytes(allTables)).ToArray());
                return allTables.Count;
            }
            catch (Exception ex)
            {
                Log.Error?.Write(ex.ToString());
            }

            return 0;
        }

        /// <summary> 构建配置文件 </summary>
        public static Dictionary<string, List<IConfigFile>> BuildFiles(string[] sourceDirectories)
        {
            var allFiles = new Dictionary<string, List<IConfigFile>>(256);
            var allConfigBuilders = GetConfigBuilders();
            foreach (var sourceDirectory in sourceDirectories)
            {
                foreach (var filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
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
                        var strbuf = StringFLibUtility.GetStrBuf();
                        var file = new ConfigBuilderFile() { Path = filePath, Builder = builder };
                        var argStartIndex = 0;
                        var sign = '*';
                        for (var i = 0; i < name.Length; i++)
                        {
                            switch (name[i])
                            {
                                case '$':
                                    sign = name[++i];
                                    break;
                                case '.' when argStartIndex == 0:
                                    argStartIndex = i + 1;
                                    break;
                                case '.':
                                    (file.Args ??= new List<string>()).Add(name[argStartIndex..]);
                                    argStartIndex = 0;
                                    break;
                                default:
                                {
                                    if (argStartIndex == 0)
                                        strbuf.Append(name[i]);
                                    break;
                                }
                            }
                        }

                        if (!ConfigBuilderUtility.CheckSign(sign, Sign)) // 依据文件标记被忽略
                            continue;

                        if (argStartIndex > 0)
                            (file.Args ??= new List<string>()).Add(name[argStartIndex..]);
                        name = file.Name = StringFLibUtility.ReleaseStrBufAndResult(strbuf);
                        if (!allFiles.TryGetValue(name, out var files))
                            allFiles.Add(name, files = new List<IConfigFile>());
                        files.Add(file);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{filePath}", e);
                    }
                }
            }

            foreach (var allFile in allFiles)
            {
                allFile.Value.TrimExcess();
                allFile.Value.Sort((a, b) => a.Path.Length - b.Path.Length);
            }

            return allFiles;
        }

        /// <summary> 构建配置表 </summary>
        public static ConcurrentDictionary<Type, IConfigTable> BuildTables(Dictionary<string, List<IConfigFile>> allFiles)
        {
            var tables = new ConcurrentDictionary<Type, IConfigTable>(Environment.ProcessorCount, allFiles.Count);

            CustomBuilder?.Invoke(tables, allFiles);

            GetAllTypes().AsParallel().ForAll(configType =>
            {
                var attr = configType.GetCustomAttribute<ConfigAttribute>();
                if (attr?.ConfigFileName == null)
                    return;
                if (!allFiles.TryGetValue(attr.ConfigFileName, out var files))
                {
                    Log.Info?.Write($"not found config file {attr.ConfigFileName}");
                    return;
                }

                var table = (ConfigBuilderTable)tables.GetOrAdd(configType, new ConfigBuilderTable(attr.ConfigFileName, configType, attr.Options));

                foreach (var item in files)
                {
                    var file = (ConfigBuilderFile)item;
                    try
                    {
                        if (typeof(IConfigFileCustomBuildToTable).IsAssignableFrom(configType))
                        {
                            var deserializer = (IConfigFileCustomBuildToTable)TypeAssistant.New(configType);
                            deserializer.ConfigFileDeserializeToTable(Sign, file, table, allFiles, tables);
                        }
                        else
                        {
                            if (file.Builder == null)
                                Log.Error?.Write($"not found config builder {configType} {attr.ConfigFileName} {file}");
                            else
                                file.Builder.Build(table, file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error?.Write($"{table}\n{ex}");
                    }
                }
            });

            return tables;
        }

        /// <summary> 构建配置表最后一步处理 </summary>
        private static void PostBuildProcess(Dictionary<string, List<IConfigFile>> allFiles, ConcurrentDictionary<Type, IConfigTable> allTables)
        {
            foreach (var item in ConfigPostBuildProcessData.AdditionConfigPostBuildProcesses)
            {
                if (item.CfgType == null)
                    item.Process.OnConfigPostBuildProcess(Sign, null, allFiles, allTables);
                else if (allTables.TryGetValue(item.CfgType, out var table))
                    item.Process.OnConfigPostBuildProcess(Sign, table, allFiles, allTables);
                else
                    throw new Exception($"not found config {item.CfgType}");
            }

            allTables.AsParallel().ForAll(item =>
            {
                var table = item.Value;
                if (typeof(IConfigPostBuildProcessable).IsAssignableFrom(table.ConfigType))
                {
                    try
                    {
                        if (table.AllConfigs.Count > 1024)
                        {
                            table.AllConfigs.AsParallel().ForAll(configs => ProcessConfigPostBuild(configs.Key, configs.Value, table, allFiles, allTables));
                        }
                        else
                        {
                            foreach (var config in table.AllConfigs)
                                ProcessConfigPostBuild(config.Key, config.Value, table, allFiles, allTables);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error?.Write($"{table}\n{ex}");
                    }
                }
            });
            return;

            static void ProcessConfigPostBuild(uint id, IBytesPackable cfg, IConfigTable table, Dictionary<string, List<IConfigFile>> allFiles, ConcurrentDictionary<Type, IConfigTable> allTables)
            {
                try
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    ((IConfigPostBuildProcessable)cfg).OnConfigPostBuildProcess(Sign, table, allFiles, allTables);
                }
                catch (Exception ex)
                {
                    Log.Error?.Write($"{id}.{table}\n{ex}");
                }
            }
        }

        /// <summary> 将构建的配置表序列化 </summary>
        private static BytesWriter GenerateConfigBytes(ConcurrentDictionary<Type, IConfigTable> allTables)
        {
            var writer = new BytesWriter();
            writer.Allocate(allTables.Count * 8192);
            writer.PushLength(allTables.Count);

            var packageBuffer = new BytesWriter(new byte[8192]);
            foreach (var tableKv in allTables.OrderBy(v => v.Key.MetadataToken))
            {
                var table = (ConfigBuilderTable)tableKv.Value;
                var options = table.Options;
                writer.Push(TypeAssistant.GetTypeName(tableKv.Key), Encoding.ASCII);
                var count = table.ConfigCount;
                writer.PushLength(count);
                if (count == 0)
                    continue;

                IEnumerable<KeyValuePair<uint, IBytesPackable>> configs = table.AllConfigs;
                if ((tableKv.Value.Options & ConfigHelper.EOption.OrderById) != 0)
                    configs = configs.OrderBy(v => v.Key);
                foreach (var idWithConfig in configs)
                {
                    packageBuffer.Clear();
                    BytesPack.Pack(idWithConfig.Value, ref packageBuffer);
                    var copyOptions = options;
                    if (packageBuffer.Length >= ConfigTableCompressSize)
                        copyOptions |= ConfigHelper.EOption.AlwaysCompressRawData;
                    if ((copyOptions & ConfigHelper.EOption.AlwaysCompressRawData) != 0)
                        packageBuffer = new BytesWriter(Compressor.Compress(packageBuffer));
                    writer.PushVInt(idWithConfig.Key);
                    writer.Push(copyOptions);
                    writer.Push(packageBuffer.Span);
                }
            }

            return writer;
        }
    }
}