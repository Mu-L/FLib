namespace FLib.Tests;

public class TestFEvent
{
    [Fact]
    public void PostEventOnceListenerIsRemovedAfterDispatch()
    {
        var evt = new FEvent();
        var count = 0;
        FEvent.PostEventHandler<int> handler = (_, in _) => count++;

        evt.ListenEvent(handler, isListenOnce: true);
        evt.DispatchEvent(1);
        evt.DispatchEvent(1);

        Assert.Equal(1, count);
        Assert.False(evt.IsListenEvent(handler));
    }

    [Fact]
    public void PostEventOnceListenerIsNotInvokedByReentrantDispatch()
    {
        var evt = new FEvent();
        var count = 0;
        FEvent.PostEventHandler<int> handler = (_, in _) =>
        {
            count++;
            if (count == 1)
                evt.DispatchEvent(1);
        };

        evt.ListenEvent(handler, isListenOnce: true);
        evt.DispatchEvent(1);

        Assert.Equal(1, count);
        Assert.False(evt.IsListenEvent(handler));
    }

    [Fact]
    public void PreEventOnceListenerIsRemovedWhenDispatchStops()
    {
        var evt = new FEvent();
        var count = 0;
        FEvent.PreEventHandler<int> handler = (_, ref _) =>
        {
            count++;
            return false;
        };

        evt.ListenPreEvent(handler, isListenOnce: true);
        var value = 1;

        Assert.False(evt.DispatchPreEvent(ref value));
        Assert.False(evt.IsListenEvent(handler));

        value = 2;
        Assert.True(evt.DispatchPreEvent(ref value));
        Assert.Equal(1, count);
    }
}
