// ==================== qcbf@qq.com | 2025-07-01 ====================

#if HYBRIDCLR
namespace FLib.Unity.Editor.PackBuilder.Task.Script
{
    public class Code : TaskBase
    {
        
    }
}
#endif



// #if HYBRIDCLR
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using HybridCLR.Editor;
// using HybridCLR.Editor.AOT;
// using HybridCLR.Editor.Commands;
// using HybridCLR.Editor.Meta;
// using HybridCLR.Editor.Settings;
// using UnityEditor;
// using UnityEditor.Build;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.UIElements;
// using Toggle = UnityEngine.UIElements.Toggle;
//
// namespace FLib.Unity.Editor
// {
//     public class CodeBuilderTask : BuilderTaskBase
//     {
//         public PlayerPrefsIntField IsBuildAot = new("BuilderBuildAot", 1);
//         public override string Name => "代码";
//
//
//         public override void Serialize(Dictionary<string, string> data)
//         {
//             base.Serialize(data);
//             data[nameof(IsBuildAot)] = IsBuildAot.Get() == 1 ? "1" : "0";
//         }
//
//         public override void Deserialize(Dictionary<string, string> data)
//         {
//             base.Deserialize(data);
//             IsBuildAot.Set(data.GetValueOrDefault(nameof(IsBuildAot)) == "1");
//         }
//
//         public override void CreateContentGUI(VisualElement root)
//         {
//             new Toggle("构建Aot").ShortFieldLabel().BindDataWithUI(v => IsBuildAot.Set(v), () => IsBuildAot).AddToUI(root);
//         }
//
//         public override void BuildProcess(BuilderContext context)
//         {
//             if (IsBuildAot)
//             {
//                 GenerateAll();
//             }
//             else
//             {
//                 CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget, Utility.IsDevelop);
//             }
//
//             var hotAssemblyNames = HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions.Select(v => v.name).Concat(HybridCLRSettings.Instance.hotUpdateAssemblies);
//             CopyDlls(Utility.AssetCachePlatformAllPath, context, "hotDlls", HybridCLRSettings.Instance.hotUpdateDllCompileOutputRootDir, hotAssemblyNames);
//             CopyDlls(Utility.AssetCachePlatformAllPath, context, "aotDlls", HybridCLRSettings.Instance.strippedAOTDllOutputRootDir, GetAotPatchDlls());
//         }
//
//
//         /// <summary>
//         ///
//         /// </summary>
//         public void GenerateAll()
//         {
//             if (!Directory.Exists($"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr"))
//                 throw new BuildFailedException($"You have not initialized HybridCLR, please install it via menu 'HybridCLR/Installer'");
//             var target = EditorUserBuildSettings.activeBuildTarget;
//             CompileDllCommand.CompileDll(target, Utility.IsDevelop);
//             Il2CppDefGeneratorCommand.GenerateIl2CppDef();
//             LinkGeneratorCommand.GenerateLinkXml(target);
//             StripAOTDllCommand.GenerateStripedAOTDlls(target);
//             MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
//             AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
//         }
//
//
//         /// <summary>
//         ///
//         /// </summary>
//         public static void CopyDlls(string dst, BuilderContext context, string type, string assemblyDir, IEnumerable<string> assembliesNames)
//         {
//             dst = Path.Combine(dst, type);
//             type = type.ToLowerInvariant();
//             FIO.ClearDirectory(dst);
//             var baseDir = Path.Combine(assemblyDir, Utility.Platform.ToString());
//             var allDlls = new List<string>();
//             foreach (var dllName in assembliesNames)
//             {
//                 var destFilePath = Path.Combine(dst, dllName + AssetLoader.NON_BUNDLE_EXTENSION);
//                 var sourceFileBytes = Compressor.Compress(File.ReadAllBytes(Path.Combine(baseDir, dllName + ".dll")));
//                 var dllAssetPath = $"{type}/{dllName.ToLowerInvariant()}{AssetLoader.NON_BUNDLE_EXTENSION}";
//                 context.GetInfoAssetMeta(dllAssetPath) = new AssetLoaderInfo.Meta
//                 {
//                     Size = sourceFileBytes.Length,
//                     Hash = new Hash128(9, CRC64.Encode(MD5.Encode(sourceFileBytes))),
//                 };
//                 allDlls.Add(dllAssetPath);
//                 File.WriteAllBytes(destFilePath, sourceFileBytes.ToArray());
//             }
//
//             context.GetInfoAssetMeta($"{type}~").Dependencies = allDlls.ToArray();
//         }
//
//
//         /// <summary>
//         ///
//         /// </summary>
//         public static IEnumerable<string> GetAotPatchDlls()
//         {
//             var hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
//             var collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(EditorUserBuildSettings.activeBuildTarget, hotUpdateDllNames), hotUpdateDllNames);
//             var analyzer = new Analyzer(new Analyzer.Options
//             {
//                 MaxIterationCount = Math.Min(20, SettingsUtil.HybridCLRSettings.maxGenericReferenceIteration),
//                 Collector = collector,
//             });
//             analyzer.Run();
//             return analyzer.AotGenericTypes.Select(v => v.Type.Module.Name).Concat(analyzer.AotGenericMethods.Select(v => v.Method.Module.Name)).Select(v => FIO.RemoveExtension(v)).Distinct();
//         }
//     }
// }
// #endif
