using System.Runtime.CompilerServices;

namespace FpZip.Core;

/// <summary>
/// Provides bijective mapping between floating-point values and unsigned integers.
/// This transform converts IEEE 754 representation to a form where larger float values
/// map to larger uint values, enabling efficient prediction-based compression.
/// </summary>
public static class PCMap
{
    /// <summary>
    /// Maps a float to a uint in a way that preserves ordering.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Forward(float d)
    {
        uint r = BitConverter.SingleToUInt32Bits(d);
        r = ~r;
        // For full precision (32 bits), shift = 0, so this simplifies to:
        // r ^= -(r >> 31) >> 1
        r ^= unchecked((uint)(-(int)(r >> 31))) >> 1;
        return r;
    }

    /// <summary>
    /// Maps a uint back to a float (inverse of Forward).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Inverse(uint r)
    {
        // For full precision (32 bits), shift = 0, so this simplifies to:
        // r ^= -(r >> 31) >> 1
        r ^= unchecked((uint)(-(int)(r >> 31))) >> 1;
        r = ~r;
        return BitConverter.UInt32BitsToSingle(r);
    }

    /// <summary>
    /// Maps a double to a ulong in a way that preserves ordering.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Forward(double d)
    {
        ulong r = BitConverter.DoubleToUInt64Bits(d);
        r = ~r;
        // For full precision (64 bits), shift = 0, so this simplifies to:
        // r ^= -(r >> 63) >> 1
        r ^= unchecked((ulong)(-(long)(r >> 63))) >> 1;
        return r;
    }

    /// <summary>
    /// Maps a ulong back to a double (inverse of Forward).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Inverse(ulong r)
    {
        // For full precision (64 bits), shift = 0, so this simplifies to:
        // r ^= -(r >> 63) >> 1
        r ^= unchecked((ulong)(-(long)(r >> 63))) >> 1;
        r = ~r;
        return BitConverter.UInt64BitsToDouble(r);
    }
}
