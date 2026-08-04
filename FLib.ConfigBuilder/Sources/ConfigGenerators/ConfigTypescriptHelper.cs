using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using FLib;

public static class ConfigTypescriptHelper
{
    private sealed class NodeWorker : IDisposable
    {
        private readonly object _sync = new();
        private readonly Process _process;

        public NodeWorker(string workingDirectory)
        {
            _process = Process.Start(new ProcessStartInfo("node", "./tools/Compile.ts")
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
            })!;
        }

        public bool IsAlive => !_process.HasExited;

        public string Compile(string path)
        {
            lock (_sync)
            {
                _process.StandardInput.WriteLine(path);
                var result = _process.StandardOutput.ReadLine()
                             ?? throw new Exception($"not found node compile result\n{path}");
                return result.StartsWith("ERROR", StringComparison.Ordinal)
                    ? throw new Exception(result)
                    : result;
            }
        }

        public void Dispose() => _process.Dispose();
    }

    private static readonly ConcurrentQueue<NodeWorker> IdleWorkers = new();
    private static readonly ConcurrentBag<NodeWorker> AllWorkers = new();
    private static readonly SemaphoreSlim Throttle = new(Environment.ProcessorCount, Environment.ProcessorCount);

    public static string Compile(string path)
    {
        Throttle.Wait();
        var worker = RentWorker(path);
        var healthy = true;
        try
        {
            return worker.Compile(path);
        }
        catch
        {
            healthy = false;
            throw;
        }
        finally
        {
            if (healthy)
                IdleWorkers.Enqueue(worker);
            else
                worker.Dispose(); // 崩溃的 worker 直接丢弃,不放回池
            Throttle.Release();
        }
    }

    private static NodeWorker RentWorker(string path)
    {
        while (IdleWorkers.TryDequeue(out var worker))
        {
            if (worker.IsAlive)
                return worker;
            worker.Dispose();
        }

        var newWorker = new NodeWorker(IOUtility.PathTrimRightDirectory(path, 2));
        AllWorkers.Add(newWorker);
        return newWorker;
    }

    public static void Close()
    {
        IdleWorkers.Clear();
        foreach (var w in AllWorkers)
            w.Dispose();
    }
}