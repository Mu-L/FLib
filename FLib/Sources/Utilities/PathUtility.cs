//==================={By Qcbf|qcbf@qq.com}===================

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace FLib
{
    public static class PathUtility
    {
        private static readonly string[] SizeNames = { "Byte", "KB", "MB", "GB", "TB" };

        private const int CompareBufferSize = 81920;

        /// <summary>
        /// 当前基础目录末尾+/
        /// </summary>
        public static string CurrentBaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 当前工作目录末尾没有/
        /// </summary>
        public static string CurrentWorkDirectory => Environment.CurrentDirectory;

        /// <summary>
        /// 清除目录
        /// </summary>
        public static void ClearDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                foreach (var item in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)) 
                    File.Delete(item);

                foreach (var item in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly)) 
                    Directory.Delete(item, true);
            }
        }

        /// <summary>
        /// 复制目录
        /// </summary>
        public static void CopyDirectory(string src, string dest, string searchPattern = "*", Func<string, bool, bool> filter = null, Action<string, string> copyHandler = null)
        {
            if (!Directory.Exists(dest)) 
                Directory.CreateDirectory(dest);

            foreach (var item in Directory.EnumerateFiles(src, searchPattern, SearchOption.TopDirectoryOnly))
            {
                if (filter?.Invoke(item, false) != false)
                {
                    var destPath = Path.Combine(dest, Path.GetFileName(item));
                    if (copyHandler != null)
                        copyHandler(item, destPath);
                    else
                        File.Copy(item, destPath, true);
                }
            }

            foreach (var item in Directory.EnumerateDirectories(src, "*", SearchOption.TopDirectoryOnly))
            {
                if (filter?.Invoke(item, true) != false) 
                    CopyDirectory(item, Path.Combine(dest, Path.GetFileName(item)), searchPattern, filter, copyHandler);
            }
        }

        /// <summary>
        /// 裁剪右边路径
        /// </summary>
        /// <param name="path">原始路径</param>
        /// <param name="count">层级次数</param>
        public static string PathTrimRightDirectory(string path, int count)
        {
            var endIndex = path.Length;
            if (IsDirectorySeparator(path[^1]))
                --endIndex;

            for (var i = endIndex - 1; i >= 0; --i)
            {
                if (IsDirectorySeparator(path[i]))
                {
                    if (--count <= 0)
                        return path[..(i + 1)];

                    endIndex = i;
                }
            }

            return path[..endIndex];
        }


        /// <summary>
        /// 裁剪左边路径
        /// </summary>
        /// <param name="path">原始路径</param>
        /// <param name="count">层级次数</param>
        public static string PathTrimLeftDirectory(string path, int count)
        {
            if (path.Length <= 2) return path;
            var startIndex = IsDirectorySeparator(path[^1]) ? 1 : 0;

            for (var i = startIndex; count > 0 && i < path.Length; ++i)
            {
                if (IsDirectorySeparator(path[i]))
                {
                    startIndex = i + 1;
                    --count;
                }
            }

            return path[startIndex..];
        }

        /// <summary>
        /// 裁剪到第几个目录
        /// </summary>
        /// <param name="leftCount">从左边第几个目录</param>
        /// <param name="path">目录路径</param>
        public static string PathTrimToDirectionName(string path, int leftCount)
        {
            if (path.Length <= 2) return path;

            var startIndex = IsDirectorySeparator(path[0]) ? 1 : 0;
            for (var i = startIndex; i < path.Length; ++i)
            {
                var c = path[i];
                if (IsDirectorySeparator(c))
                {
                    if (--leftCount <= 0 || i == path.Length - 1)
                        return path[startIndex..i];

                    startIndex = i + 1;
                }
            }

            return path[startIndex..];
        }

        /// <summary>
        /// 
        /// </summary>
        public static ReadOnlySpan<char> PathTrimToDirectionName(ReadOnlySpan<char> path, ReadOnlySpan<char> directionName, bool containsDirectionName = false)
        {
            if (IsDirectorySeparator(path[^1]))
                path = path[..^1];
            var endIndex = path.Length;
            for (var i = path.Length - 1; i >= 0; i--)
            {
                if (IsDirectorySeparator(path[i]))
                {
                    if (path[(i + 1)..endIndex].Equals(directionName, StringComparison.Ordinal))
                        return containsDirectionName ? path[..endIndex] : path[..i];
                    endIndex = i;
                }
            }

            return default;
        }

        /// <summary>
        /// 获取一个格式化的文件大小字符串
        /// </summary>
        public static string FormatSize(double size)
        {
            var order = 0;
            while (size >= 1024 && order < SizeNames.Length - 1)
            {
                order++;
                size /= 1024f;
            }

            return size.ToString("0.##") + SizeNames[order];
        }


        /// <summary>
        /// 获取一个安全的文件名,不会重名
        /// </summary>
        public static string SafePath(bool isFilePath, string path, string splitChar = "-", int minSuffixLength = 0)
        {
            var oldPath = path;
            if (minSuffixLength > 0)
            {
                var tempDotIndex = path.LastIndexOf('.');
                var suffix = new string('0', minSuffixLength);
                path = tempDotIndex >= 0 ? path.Insert(tempDotIndex, splitChar + suffix) : path + splitChar + suffix;
            }

            if ((isFilePath && !File.Exists(path)) || (!isFilePath && !Directory.Exists(path)))
            {
                return path;
            }

            path = oldPath;

            var path2 = path;
            var extension = string.Empty;
            var dotIndex = path.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                extension = path[dotIndex..];
                path2 = path2[..dotIndex];
            }

            var strbuf = StringFLibUtility.GetStrBuf(path2.Length + splitChar.Length + minSuffixLength + extension.Length + 10);
            strbuf.Append(path2).Append(splitChar);
            var path2Count = path2.Length + splitChar.Length;
            try
            {
                for (var i = 1; i < int.MaxValue; i++)
                {
                    var digitCount = GetDigitCount(i);
                    if (digitCount < minSuffixLength)
                        strbuf.Append('0', minSuffixLength - digitCount);

                    strbuf.Append(i);
                    var newPath = strbuf.Append(extension).ToString();
                    strbuf.Remove(path2Count, strbuf.Length - path2Count);
                    if ((isFilePath && !File.Exists(newPath)) || (!isFilePath && !Directory.Exists(newPath)))
                    {
                        return newPath;
                    }
                }
            }
            finally
            {
                StringFLibUtility.ReleaseStrBuf(strbuf);
            }

            throw new Exception("not found new filepath");
        }


        /// <summary>
        /// 修改路径文件的名称
        /// </summary>
        public static string PathRename(string path, string newName, bool isAppendNewName = false, bool isKeepExtension = true)
        {
            if (!isAppendNewName)
            {
                var dirIndex = path.LastIndexOf('/');
                if (dirIndex == -1)
                {
                    dirIndex = path.LastIndexOf('\\');
                }

                if (dirIndex == -1)
                {
                    return newName;
                }

                newName = path[..(dirIndex + 1)] + newName;
                if (!isKeepExtension) return newName;
            }

            var exIndex = path.LastIndexOf('.');
            if (exIndex >= 0)
            {
                if (isAppendNewName)
                {
                    newName = path[..exIndex] + newName;
                }

                if (isKeepExtension) newName += path[exIndex..];
            }
            else if (isAppendNewName)
            {
                newName = path + newName;
            }

            return newName;
        }

        /// <summary>
        /// 移除后缀名
        /// </summary>
        public static string RemoveExtension(string path)
        {
            for (var i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '.')
                {
                    return path[..i];
                }
                else if (path[i] == '/' || path[i] == '\\')
                {
                    break;
                }
            }

            return path;
        }

        /// <summary>
        /// 获取文件目录
        /// </summary>
        public static string GetFileDirectory(string filepath)
        {
            return File.Exists(filepath) ? Path.GetDirectoryName(filepath) : filepath;
        }

        /// <summary>
        /// 比较两个流
        /// </summary>
        public static bool Compare(string path1, string path2)
        {
            if (!File.Exists(path1) || !File.Exists(path2)) return false;
            using var a = File.Open(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var b = File.Open(path2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Compare(a, b);
        }

        /// <summary>
        /// 比较两个流
        /// </summary>
        public static bool Compare(Stream a, Stream b)
        {
            if (a.Length != b.Length) return false;

            var buffer1 = ArrayPool<byte>.Shared.Rent(CompareBufferSize);
            var buffer2 = ArrayPool<byte>.Shared.Rent(CompareBufferSize);
            try
            {
                var remaining = a.Length;
                while (remaining > 0)
                {
                    var readCount = (int)Math.Min(remaining, CompareBufferSize);
                    if (!ReadFully(a, buffer1, readCount) || !ReadFully(b, buffer2, readCount))
                        return false;

                    if (!buffer1.AsSpan(0, readCount).SequenceEqual(buffer2.AsSpan(0, readCount)))
                        return false;

                    remaining -= readCount;
                }

                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer1);
                ArrayPool<byte>.Shared.Return(buffer2);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static FileStream OpenTempFile(string extension = ".txt")
        {
            var tempDirectory = Path.GetTempPath();
            while (true)
            {
                var path = Path.Combine(tempDirectory, Path.GetRandomFileName() + extension);
                try
                {
                    return File.Open(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                catch (IOException) when (File.Exists(path))
                {
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CreateZip(string[] paths, string zipFilePath, Regex excludePatterns = null, CompressionLevel level = CompressionLevel.Optimal)
        {
            using var zipStream = new FileStream(zipFilePath!, FileMode.Create);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            foreach (var path in paths)
            {
                var entryName = Path.GetFileName(path);
                if (excludePatterns?.Match(entryName).Success == true)
                    continue;

                if (File.Exists(path))
                {
                    archive.CreateEntryFromFile(path, entryName, level);
                }
                else if (Directory.Exists(path))
                {
                    entryName = Path.GetFullPath(path);
                    var baseDirLen = entryName.Length;
                    if (!entryName.EndsWith(Path.DirectorySeparatorChar))
                        ++baseDirLen;
                    foreach (var dirPath in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        entryName = dirPath[baseDirLen..];
                        if (excludePatterns?.Match(entryName).Success == true)
                            continue;
                        archive.CreateEntryFromFile(dirPath, entryName, level);
                    }
                }
                else
                {
                    Log.Warn?.Write($"警告：路径不存在，已跳过 {path}");
                }
            }
        }

        private static bool IsDirectorySeparator(char c) => c is '/' or '\\';

        private static int GetDigitCount(int value)
        {
            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1000) return 3;
            if (value < 10000) return 4;
            if (value < 100000) return 5;
            if (value < 1000000) return 6;
            if (value < 10000000) return 7;
            if (value < 100000000) return 8;
            if (value < 1000000000) return 9;
            return 10;
        }

        private static bool ReadFully(Stream stream, byte[] buffer, int count)
        {
            var offset = 0;
            while (offset < count)
            {
                var readCount = stream.Read(buffer, offset, count - offset);
                if (readCount == 0)
                    return false;

                offset += readCount;
            }

            return true;
        }
    }
}
