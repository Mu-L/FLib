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
        public static Action<ConcurrentDictionary<Type, IConfigTable>, ConcurrentDictionary<string, ConcurrentBag<IConfigFile>>> CustomBuilder;

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
                File.WriteAllBytes(outPath, GenerateConfigBytes(allTables).Span.ToArray());
                return allTables.Count;
            }
            catch (Exception ex)
            {
                Log.Error?.Write(ex.ToString());
            }

            return 0;
        }

        /// <summary> 构建配置文件 </summary>
        public static ConcurrentDictionary<string, ConcurrentBag<IConfigFile>> BuildFiles(string[] sourceDirectories)
        {
            var allFiles = new ConcurrentDictionary<string, ConcurrentBag<IConfigFile>>(Environment.ProcessorCount, 1024);
            var allConfigBuilders = GetConfigBuilders();
            var strbuf = new StringBuilder();
            foreach (var sourceDirectory in sourceDirectories)
            {
                Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories).AsParallel().ForAll(filePath =>
                {
                    try
                    {
                        if (filePath.EndsWith(".meta", StringComparison.Ordinal))
                            return;
                        var name = Path.GetFileNameWithoutExtension(filePath);
                        if (name.StartsWith('~') || name.StartsWith('_') || name.EndsWith('~') || name.EndsWith(".schema", StringComparison.Ordinal) || name.EndsWith(".ai", StringComparison.Ordinal))
                            return;
                        if (!allConfigBuilders.TryGetValue(Path.GetExtension(filePath), out var builder))
                            return;
                        var file = new ConfigBuilderFile() { FileSign = '*', Path = filePath, Builder = builder };
                        var argStartIndex = 0;
                        for (var i = 0; i < name.Length; i++)
                        {
                            switch (name[i])
                            {
                                case '$':
                                    file.FileSign = name[++i]; // step $
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
                                    {
                                        strbuf.Append(name[i]);
                                    }

                                    break;
                                }
                            }
                        }

                        if (argStartIndex > 0)
                            (file.Args ??= new List<string>()).Add(name[argStartIndex..]);

                        name = file.Name = strbuf.ToString();
                        strbuf.Clear();

                        var files = allFiles.GetOrAdd(name, static _ => new ConcurrentBag<IConfigFile>());
                        files.Add(file);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{filePath}", e);
                    }
                });
            }

            return allFiles;
        }

        /// <summary> 构建配置表 </summary>
        public static ConcurrentDictionary<Type, IConfigTable> BuildTables(ConcurrentDictionary<string, ConcurrentBag<IConfigFile>> allFiles)
        {
            var tables = new ConcurrentDictionary<Type, IConfigTable>(Environment.ProcessorCount, 1024);

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

                foreach (var item in files)
                {
                    var file = (ConfigBuilderFile)item;
                    if (!ConfigBuilderUtility.CheckSign(file.FileSign, Sign))
                        continue;
                    var table = (ConfigBuilderTable)tables.GetOrAdd(configType, new ConfigBuilderTable(attr.ConfigFileName, configType, attr.Options));
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
        private static void PostBuildProcess(ConcurrentDictionary<string, ConcurrentBag<IConfigFile>> allFiles, ConcurrentDictionary<Type, IConfigTable> allTables)
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
                        if (table.AllConfigIdIndexes.Count > 1024)
                        {
                            table.AllConfigIdIndexes.AsParallel().ForAll(indexes =>
                            {
                                foreach (var index in indexes.Value)
                                    ProcessConfigPostBuild(indexes.Key, table.AllConfigs[index], table, allFiles, allTables);
                            });
                        }
                        else
                        {
                            foreach (var indexes in table.AllConfigIdIndexes)
                            {
                                foreach (var index in indexes.Value)
                                    ProcessConfigPostBuild(indexes.Key, table.AllConfigs[index], table, allFiles, allTables);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error?.Write($"{table}\n{ex}");
                    }
                }
            });
            return;

            static void ProcessConfigPostBuild(uint id, IBytesPackable cfg, IConfigTable table, ConcurrentDictionary<string, ConcurrentBag<IConfigFile>> allFiles, ConcurrentDictionary<Type, IConfigTable> allTables)
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

            var packageBuffer = new BytesWriter();
            foreach (var ctx in allTables.OrderBy(v => v.Key.MetadataToken))
            {
                var table = (ConfigBuilderTable)ctx.Value;
                var options = table.Options;
                writer.Push(TypeAssistant.GetTypeName(ctx.Key), Encoding.ASCII);
                var count = table.ConfigCount;
                writer.PushLength(count);
                if (count == 0)
                    continue;

                IEnumerable<KeyValuePair<uint, List<int>>> configs = table.AllConfigIdIndexes;
                if ((ctx.Value.Options & ConfigHelper.EOption.OrderById) != 0)
                    configs = configs.OrderBy(v => v.Key);
                foreach (var indexes in configs)
                {
                    packageBuffer.Clear();
                    foreach (var index in indexes.Value)
                        BytesPack.Pack(table.AllConfigs[index], ref packageBuffer);
                    var copyOptions = options;
                    if (packageBuffer.Length >= ConfigTableCompressSize)
                        copyOptions |= ConfigHelper.EOption.AlwaysCompressRawData;
                    if ((copyOptions & ConfigHelper.EOption.AlwaysCompressRawData) != 0)
                        packageBuffer = new BytesWriter(Compressor.Compress(packageBuffer));
                    writer.PushVInt(indexes.Key);
                    writer.Push(copyOptions);
                    writer.Push(packageBuffer.Span);
                }
            }

            return writer;
        }
    }
}