namespace DiligentEngineNET.Samples.Utils;

public class Synchro(int threadsCount, CancellationToken token)
{
    private CountdownEvent _countdown = new (threadsCount);

    private ManualResetEventSlim[] _events =
    [
        new (false),
        new (false),
    ];

    private readonly Lock _sync = new();
    private volatile int _nextEvtIdx;

    public void MarkThreadAsReady()
    {
        _countdown.Signal();
    }

    public void Wait()
    {
        int nextEvtIdx;
        lock(_sync)
            nextEvtIdx = _nextEvtIdx;
        
        _events[nextEvtIdx].Wait(1000);
    }

    public void WaitSignal()
    {
        MarkThreadAsReady();
        Wait();
    }

    public void Signal()
    {
        WaitForThreads();
        lock (_sync)
        {
            var currEvt = _events[_nextEvtIdx];
            // ReSharper disable once NonAtomicCompoundOperator
            _nextEvtIdx++;
            // ReSharper disable once NonAtomicCompoundOperator
            _nextEvtIdx %= _events.Length;
            
            _countdown.Reset();
            _events[_nextEvtIdx].Reset();
            currEvt.Set();
        }
    }

    public void WaitForThreads()
    {
        _countdown.Wait();
    }
}