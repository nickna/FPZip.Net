using System.Buffers;
using System.Runtime.CompilerServices;

namespace FpZip.Coding;

/// <summary>
/// Range coder (arithmetic decoder) for entropy decoding.
/// Maintains an interval [low, low+range) and a code value.
/// </summary>
public sealed class RangeDecoder : IDisposable
{
    private const int BufferSize = 4096;

    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private readonly int _bufferCapacity;
    private int _bufferPos;
    private int _bufferSize;
    private long _bytesRead;
    private uint _low;
    private uint _range;
    private uint _code;

    public bool Error { get; private set; }

    /// <summary>
    /// Gets the total number of bytes read.
    /// </summary>
    public long BytesRead => _bytesRead;

    /// <summary>
    /// Creates a new range decoder reading from the specified stream.
    /// </summary>
    public RangeDecoder(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        _bufferCapacity = BufferSize; // Track actual capacity we use, not rented size
        _bufferPos = 0;
        _bufferSize = 0;
        _bytesRead = 0;
        _low = 0;
        _range = 0xFFFFFFFFu; // -1u in unsigned
        _code = 0;
        Error = false;
    }

    /// <summary>
    /// Initializes the decoder by reading the first 4 bytes.
    /// </summary>
    public void Init()
    {
        Error = false;
        Get(4);
    }

    /// <summary>
    /// Decodes a single bit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DecodeBit()
    {
        _range >>= 1;
        bool s = (_code >= _low + _range);
        if (s)
            _low += _range;
        Normalize();
        return s;
    }

    /// <summary>
    /// Decodes a symbol using a probability model.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Decode(RCQsModel model)
    {
        model.Normalize(ref _range);
        uint l = (_code - _low) / _range;
        uint s = model.Decode(ref l, out uint r);
        _low += _range * l;
        _range *= r;
        Normalize();
        return s;
    }

    /// <summary>
    /// Decodes an n-bit unsigned integer (0 &lt;= result &lt; 2^n).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Decode(int n)
    {
        uint s = 0;
        int m = 0;

        while (n > 16)
        {
            s += DecodeShift(16) << m;
            m += 16;
            n -= 16;
        }

        return (DecodeShift(n) << m) + s;
    }

    /// <summary>
    /// Decodes a 64-bit unsigned integer with n bits.
    /// </summary>
    public ulong DecodeLong(int n)
    {
        ulong s = 0;
        int m = 0;

        while (n > 16)
        {
            s += (ulong)DecodeShift(16) << m;
            m += 16;
            n -= 16;
        }

        return ((ulong)DecodeShift(n) << m) + s;
    }

    /// <summary>
    /// Decodes using shift (for power-of-2 ranges).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint DecodeShift(int n)
    {
        _range >>= n;
        uint s = (_code - _low) / _range;
        _low += _range * s;
        Normalize();
        return s;
    }

    /// <summary>
    /// Decodes using division (for arbitrary ranges).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint DecodeRatio(uint n)
    {
        _range /= n;
        uint s = (_code - _low) / _range;
        _low += _range * s;
        Normalize();
        return s;
    }

    /// <summary>
    /// Normalizes the range and inputs new data.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Normalize()
    {
        // While top 8 bits of low and low+range are the same, shift them out
        while (((_low ^ (_low + _range)) >> 24) == 0)
        {
            Get(1);
            _range <<= 8;
        }

        // If range is too small, fudge to avoid carry and input 16 bits
        if ((_range >> 16) == 0)
        {
            Get(2);
            _range = unchecked((uint)(-(int)_low));
        }
    }

    /// <summary>
    /// Inputs n bytes from the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Get(int n)
    {
        for (int i = 0; i < n; i++)
        {
            _code <<= 8;
            _code |= GetByte();
            _low <<= 8;
        }
    }

    /// <summary>
    /// Reads a single byte from the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte GetByte()
    {
        if (_bufferPos >= _bufferSize)
        {
            _bufferSize = _stream.Read(_buffer, 0, _bufferCapacity);
            _bytesRead += _bufferSize;
            _bufferPos = 0;

            if (_bufferSize == 0)
            {
                // EOF - return 0 and set error
                Error = true;
                return 0;
            }
        }

        return _buffer[_bufferPos++];
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
