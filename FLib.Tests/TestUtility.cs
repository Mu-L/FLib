// // ==================== qcbf@qq.com | 2026-07-06 ====================

namespace FLib.Tests;

public static class TestUtility
{
    public static void RegisterFLibLog()
    {
        FLib.Log.GlobalOutputHandler += (log, s) => TestContext.Current.TestOutputHelper!.WriteLine(s);
    }

    public static void Log(object? content, object? tag1 = null, object? tag2 = null)
    {
        var text = FLib.Log.Combine(content, tag1, tag2, FLib.Log.EOption.AppendThreadId | FLib.Log.EOption.AppendDate);
        TestContext.Current.TestOutputHelper!.WriteLine(text);
    }
}