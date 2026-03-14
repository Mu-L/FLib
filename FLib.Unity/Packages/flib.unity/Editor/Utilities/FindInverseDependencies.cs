//==================={By Qcbf|qcbf@qq.com|8/25/2022 2:16:52 PM}===================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace FLib.Unity.Editor
{
    public static class FindInverseDependencies
    {
        private static readonly HashSet<string> FindExtensions = new(new[] { ".prefab", ".mat", ".asset" });

        public static void FindSelection(string baseDir = "")
        {
            var strbuf = new StringBuilder();
            EditorFLibUtility.SelectionProcess<UnityEngine.Object>(item =>
            {
                strbuf.Clear();
                if (item.GetType() != typeof(DefaultAsset))
                {
                    var result = Find(AssetDatabase.GetAssetPath(Selection.activeObject), baseDir);
                    strbuf.Append(item.name).Append(" [").Append(result.Count()).AppendLine("]:").AppendLine(string.Join('\n', result));
                }

                EditorFLibUtility.ClipboardTxt = strbuf.ToString();
                Log.Info?.Write(strbuf.ToString());
                return default;
            }).Forget();
        }

        public static IEnumerable<string> Find(string targetPath, string baseDir = "")
        {
            EditorUtility.DisplayProgressBar(0.ToString("p2"), "", 0);
            try
            {
                var list = new ConcurrentBag<string>();
                var guid = AssetDatabase.AssetPathToGUID(targetPath);
                var tasks = new Task[Environment.ProcessorCount];
                var allPaths = Directory.GetFiles(Path.Combine("Assets", baseDir), "*", SearchOption.AllDirectories);
                var computePathCount = (int)Math.Floor(allPaths.Length / (float)tasks.Length);
                var progress = 0;
                var isCancel = false;
                for (var i = 0; i < tasks.Length; i++)
                {
                    var startIndex = i * computePathCount;
                    var paths = new ArraySegment<string>(allPaths, startIndex, Math.Min(computePathCount, allPaths.Length - startIndex));
                    tasks[i] = Task.Run(() =>
                    {
                        var buf = new byte[MathEx.GetNextPowerOfTwo(guid.Length * 16)];
                        foreach (var path in paths)
                        {
                            if (isCancel) break;
                            Interlocked.Increment(ref progress);
                            if (!FindExtensions.Contains(Path.GetExtension(path))) continue;

                            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            var fsLen = fs.Length;
                            while (fs.Position < fsLen)
                            {
                                var count = (int)Math.Min(fsLen - fs.Position, buf.Length);
                                _ = fs.Read(buf, 0, count);
                                var str = Encoding.ASCII.GetString(buf);
                                if (str.Contains(guid))
                                {
                                    list.Add(path[baseDir.Length..]);
                                    break;
                                }
                            }
                        }
                    });
                }

                var total = (float)allPaths.Length;
                while (!tasks.All(v => v.IsCompleted))
                {
                    var p = progress / total;
                    isCancel = EditorUtility.DisplayCancelableProgressBar(p.ToString("p2"), "", p);
                }

                return list;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
