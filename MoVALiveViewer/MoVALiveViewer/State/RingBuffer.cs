using System.Collections;

namespace MoVALiveViewer.State;

public sealed class RingBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;
    private readonly object _lock = new();

    public RingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException("Capacity must be positive", nameof(capacity));
        _buffer = new T[capacity];
    }

    public int Capacity => _buffer.Length;
    public int Count { get { lock (_lock) return _count; } }

    public void Add(T item)
    {
        lock (_lock)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
        }
    }

    public T[] ToArray()
    {
        lock (_lock)
        {
            var result = new T[_count];
            int start = _count < _buffer.Length ? 0 : _head;
            for (int i = 0; i < _count; i++)
                result[i] = _buffer[(start + i) % _buffer.Length];
            return result;
        }
    }

    public T[] GetLast(int n)
    {
        lock (_lock)
        {
            n = Math.Min(n, _count);
            var result = new T[n];
            int start = (_head - n + _buffer.Length) % _buffer.Length;
            for (int i = 0; i < n; i++)
                result[i] = _buffer[(start + i) % _buffer.Length];
            return result;
        }
    }

    public T? Last()
    {
        lock (_lock)
        {
            if (_count == 0) return default;
            return _buffer[(_head - 1 + _buffer.Length) % _buffer.Length];
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _count = 0;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        var arr = ToArray();
        return ((IEnumerable<T>)arr).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
