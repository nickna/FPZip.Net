using System.Buffers;
using System.Runtime.CompilerServices;

namespace FpZip.Coding;

/// <summary>
/// Range coder (arithmetic encoder) for entropy coding.
/// Maintains an interval [low, low+range) that narrows with each encoded symbol.
/// </summary>
public sealed class RangeEncoder : IDisposable
{
    private const int BufferSize = 4096;

    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private readonly int _bufferLength;
    private int _bufferPos;
    private long _bytesWritten;
    private uint _low;
    private uint _range;

    /// <summary>
    /// Gets the total number of bytes written.
    /// </summary>
    public long BytesWritten => _bytesWritten + _bufferPos;

    /// <summary>
    /// Creates a new range encoder writing to the specified stream.
    /// </summary>
    public RangeEncoder(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        _bufferLength = BufferSize; // Track actual size we use, not rented size
        _bufferPos = 0;
        _bytesWritten = 0;
        _low = 0;
        _range = 0xFFFFFFFFu; // -1u in unsigned
    }

    /// <summary>
    /// Finishes encoding and flushes all remaining data.
    /// </summary>
    public void Finish()
    {
        // Output 4 bytes to finalize
        Put(4);
        Flush();
    }

    /// <summary>
    /// Encodes a single bit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EncodeBit(bool bit)
    {
        _range >>= 1;
        if (bit)
            _low += _range;
        Normalize();
    }

    /// <summary>
    /// Encodes a symbol using a probability model.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(uint symbol, RCQsModel model)
    {
        model.Encode(symbol, out uint l, out uint r);
        model.Normalize(ref _range);
        _low += _range * l;
        _range *= r;
        Normalize();
    }

    /// <summary>
    /// Encodes an n-bit unsigned integer (0 &lt;= s &lt; 2^n).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(uint s, int n)
    {
        if (n > 16)
        {
            EncodeShift(s & 0xFFFF, 16);
            s >>= 16;
            n -= 16;
        }
        EncodeShift(s, n);
    }

    /// <summary>
    /// Encodes a 64-bit unsigned integer with n bits.
    /// </summary>
    public void Encode(ulong s, int n)
    {
        // Handle up to 64 bits in 16-bit chunks
        while (n > 16)
        {
            EncodeShift((uint)(s & 0xFFFF), 16);
            s >>= 16;
            n -= 16;
        }
        EncodeShift((uint)s, n);
    }

    /// <summary>
    /// Encodes an integer using shift (for power-of-2 ranges).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EncodeShift(uint s, int n)
    {
        _range >>= n;
        _low += _range * s;
        Normalize();
    }

    /// <summary>
    /// Encodes an integer using division (for arbitrary ranges).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EncodeRatio(uint s, uint n)
    {
        _range /= n;
        _low += _range * s;
        Normalize();
    }

    /// <summary>
    /// Normalizes the range and outputs fixed bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Normalize()
    {
        // While top 8 bits of low and low+range are the same, output them
        while (((_low ^ (_low + _range)) >> 24) == 0)
        {
            Put(1);
            _range <<= 8;
        }

        // If range is too small, fudge to avoid carry and output 16 bits
        if ((_range >> 16) == 0)
        {
            Put(2);
            _range = unchecked((uint)(-(int)_low));
        }
    }

    /// <summary>
    /// Outputs n bytes from the high bits of low.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Put(int n)
    {
        for (int i = 0; i < n; i++)
        {
            PutByte((byte)(_low >> 24));
            _low <<= 8;
        }
    }

    /// <summary>
    /// Writes a single byte to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PutByte(byte b)
    {
        _buffer[_bufferPos++] = b;
        if (_bufferPos == _bufferLength)
            FlushBuffer();
    }

    /// <summary>
    /// Flushes the internal buffer to the stream.
    /// </summary>
    private void FlushBuffer()
    {
        if (_bufferPos > 0)
        {
            _stream.Write(_buffer, 0, _bufferPos);
            _bytesWritten += _bufferPos;
            _bufferPos = 0;
        }
    }

    /// <summary>
    /// Flushes all buffered data to the stream.
    /// </summary>
    public void Flush()
    {
        FlushBuffer();
        _stream.Flush();
    }

    public void Dispose()
    {
        Flush();
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
