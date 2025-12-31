using System.Numerics;
using System.Runtime.CompilerServices;

namespace FpZip.Core;

/// <summary>
/// Circular buffer for the 3D wavefront predictor.
/// Maintains a front of encoded but not finalized samples for prediction.
/// </summary>
/// <typeparam name="T">The numeric type (uint for float compression, ulong for double)</typeparam>
public sealed class Front<T> where T : struct
{
    private readonly T _zero;    // default value
    private readonly int _dx;    // front index x offset
    private readonly int _dy;    // front index y offset
    private readonly int _dz;    // front index z offset
    private readonly int _mask;  // index mask (power of 2 minus 1)
    private int _index;          // modular index of current sample
    private readonly T[] _buffer;

    /// <summary>
    /// Creates a new Front buffer for the given dimensions.
    /// </summary>
    /// <param name="nx">X dimension size</param>
    /// <param name="ny">Y dimension size</param>
    /// <param name="zero">Default zero value</param>
    public Front(int nx, int ny, T zero = default)
    {
        _zero = zero;
        _dx = 1;
        _dy = nx + 1;
        _dz = _dy * (ny + 1);
        _mask = ComputeMask(_dx + _dy + _dz);
        _index = 0;
        _buffer = new T[_mask + 1];
    }

    /// <summary>
    /// Fetches a neighbor relative to the current sample position.
    /// </summary>
    /// <param name="x">X offset (1 = previous x)</param>
    /// <param name="y">Y offset (1 = previous y)</param>
    /// <param name="z">Z offset (1 = previous z)</param>
    public T this[int x, int y, int z]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer[(_index - _dx * x - _dy * y - _dz * z) & _mask];
    }

    /// <summary>
    /// Adds a sample to the front.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(T value)
    {
        _buffer[_index++ & _mask] = value;
    }

    /// <summary>
    /// Adds n copies of a sample to the front.
    /// </summary>
    public void Push(T value, int count)
    {
        for (int i = 0; i < count; i++)
        {
            _buffer[_index++ & _mask] = value;
        }
    }

    /// <summary>
    /// Advances the front to (x, y, z) relative to current sample, filling with zeros.
    /// </summary>
    public void Advance(int x, int y, int z)
    {
        Push(_zero, _dx * x + _dy * y + _dz * z);
    }

    /// <summary>
    /// Computes the smallest power-of-2-minus-1 mask that is >= n-1.
    /// </summary>
    private static int ComputeMask(int n)
    {
        n--;
        // Fill all bits below the highest set bit
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n;
    }
}
