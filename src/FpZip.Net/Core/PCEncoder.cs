using System.Numerics;
using System.Runtime.CompilerServices;
using FpZip.Coding;

namespace FpZip.Core;

/// <summary>
/// Predictive coder encoder for float values (32-bit).
/// Uses wide encoding mode (bits > 8) for lossless compression.
/// </summary>
public sealed class PCEncoderFloat
{
    /// <summary>
    /// Number of symbols in the probability model (2 * 32 + 1 = 65).
    /// </summary>
    public const int Symbols = 2 * 32 + 1;

    private const int Bias = 32; // perfect prediction symbol

    private readonly RangeEncoder _encoder;
    private readonly RCQsModel _model;

    public PCEncoderFloat(RangeEncoder encoder, RCQsModel model)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Encodes a mapped value with prediction.
    /// </summary>
    /// <param name="actual">The actual mapped value (from PCMap.Forward)</param>
    /// <param name="predicted">The predicted mapped value</param>
    /// <returns>The actual value (unchanged)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Encode(uint actual, uint predicted)
    {
        // Compute (-1)^s (2^k + m) = actual - predicted
        // entropy code (s, k), and encode the k-bit number m verbatim
        if (predicted < actual)
        {
            // Underprediction
            uint d = actual - predicted;
            int k = BitScanReverse(d);
            _encoder.Encode((uint)(Bias + 1 + k), _model);
            _encoder.Encode(d - (1u << k), k);
        }
        else if (predicted > actual)
        {
            // Overprediction
            uint d = predicted - actual;
            int k = BitScanReverse(d);
            _encoder.Encode((uint)(Bias - 1 - k), _model);
            _encoder.Encode(d - (1u << k), k);
        }
        else
        {
            // Perfect prediction
            _encoder.Encode(Bias, _model);
        }

        return actual;
    }

    /// <summary>
    /// Returns the position of the highest set bit (0-indexed from LSB).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int BitScanReverse(uint x)
    {
        return 31 - BitOperations.LeadingZeroCount(x);
    }
}

/// <summary>
/// Predictive coder encoder for double values (64-bit).
/// Uses wide encoding mode (bits > 8) for lossless compression.
/// </summary>
public sealed class PCEncoderDouble
{
    /// <summary>
    /// Number of symbols in the probability model (2 * 64 + 1 = 129).
    /// </summary>
    public const int Symbols = 2 * 64 + 1;

    private const int Bias = 64; // perfect prediction symbol

    private readonly RangeEncoder _encoder;
    private readonly RCQsModel _model;

    public PCEncoderDouble(RangeEncoder encoder, RCQsModel model)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Encodes a mapped value with prediction.
    /// </summary>
    /// <param name="actual">The actual mapped value (from PCMap.Forward)</param>
    /// <param name="predicted">The predicted mapped value</param>
    /// <returns>The actual value (unchanged)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Encode(ulong actual, ulong predicted)
    {
        // Compute (-1)^s (2^k + m) = actual - predicted
        // entropy code (s, k), and encode the k-bit number m verbatim
        if (predicted < actual)
        {
            // Underprediction
            ulong d = actual - predicted;
            int k = BitScanReverse(d);
            _encoder.Encode((uint)(Bias + 1 + k), _model);
            _encoder.Encode(d - (1ul << k), k);
        }
        else if (predicted > actual)
        {
            // Overprediction
            ulong d = predicted - actual;
            int k = BitScanReverse(d);
            _encoder.Encode((uint)(Bias - 1 - k), _model);
            _encoder.Encode(d - (1ul << k), k);
        }
        else
        {
            // Perfect prediction
            _encoder.Encode(Bias, _model);
        }

        return actual;
    }

    /// <summary>
    /// Returns the position of the highest set bit (0-indexed from LSB).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int BitScanReverse(ulong x)
    {
        return 63 - BitOperations.LeadingZeroCount(x);
    }
}
