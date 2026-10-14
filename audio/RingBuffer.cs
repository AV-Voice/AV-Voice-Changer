namespace VybeShift.Core.Audio;

/// <summary>Lock-free SPSC ring buffer for float audio samples. Zero allocations after init.</summary>
public sealed class RingBuffer
{
    private readonly float[] _buf;
    private readonly int     _mask;
    private volatile int     _head = 0;
    private volatile int     _tail = 0;

    public RingBuffer(int capacity)
    {
        int size = 1;
        while (size < capacity) size <<= 1;
        _mask = size - 1;
        _buf  = GC.AllocateArray<float>(size, pinned: true);
    }

    public int  Available => (_head - _tail + _mask + 1) & _mask;
    public bool IsEmpty   => _head == _tail;

    public int Write(ReadOnlySpan<float> data)
    {
        int n = 0;
        foreach (var s in data)
        {
            int next = (_head + 1) & _mask;
            if (next == _tail) break;
            _buf[_head] = s;
            _head = next;
            n++;
        }
        return n;
    }

    public int Read(Span<float> dest)
    {
        int n = 0;
        for (int i = 0; i < dest.Length; i++)
        {
            if (_tail == _head) break;
            dest[i] = _buf[_tail];
            _tail   = (_tail + 1) & _mask;
            n++;
        }
        return n;
    }

    public void Clear() { _head = 0; _tail = 0; }
}
