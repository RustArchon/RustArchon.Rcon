using System.Numerics;

namespace RustArchon.Rcon;

public class Sequence<T> where T : INumber<T>
{
    private T _lastValue;
    private object _lock = new object();
    public T LastValue
    {
        get
        {
            lock (_lock)
            {
                return _lastValue;
            }
        }
    }
    public T GetValue()
    {
        lock (_lock)
        {
            _lastValue++;
            return _lastValue;
        }
    }

    public Sequence(T initialValue)
    {
        lock (_lock)
        {
            _lastValue = initialValue;
        }
    }
}
