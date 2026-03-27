// ==================== qcbf@qq.com | 2026-03-26 ====================

using FLib.WorldCores.TimeLogic;

namespace FLib.Tests;

public class TestSerialization
{
    [Fact]
    public void TimeLogic()
    {
        var tl = new TimeLogicRuntime
        {
            Name = "tl",
            EndFrame = 10,
            Tracks = [new TimeLogicTrack() { Name = "tlt1", Clips = [new TimeLogicClip() { Name = "tlc11", BeginFrame = 0, EndFrame = 10 }] }]
        };
        
        var json = Json5.Serialize(tl);
    }
}