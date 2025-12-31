using System.Runtime.CompilerServices;
using FpZip.Coding;

namespace FpZip.Core;

/// <summary>
/// Predictive coder decoder for float values (32-bit).
/// Uses wide decoding mode (bits > 8) for lossless decompression.
/// </summary>
public sealed class PCDecoderFloat
{
    /// <summary>
    /// Number of symbols in the probability model (2 * 32 + 1 = 65).
    /// </summary>
    public const int Symbols = 2 * 32 + 1;

    private const uint Bias = 32; // perfect prediction symbol

    private readonly RangeDecoder _decoder;
    private readonly RCQsModel _model;

    public PCDecoderFloat(RangeDecoder decoder, RCQsModel model)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Decodes a value given a prediction.
    /// </summary>
    /// <param name="predicted">The predicted mapped value</param>
    /// <returns>The actual mapped value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Decode(uint predicted)
    {
        uint s = _decoder.Decode(_model);

        if (s > Bias)
        {
            // Underprediction
            int k = (int)(s - Bias - 1);
            uint d = (1u << k) + _decoder.Decode(k);
            return predicted + d;
        }
        else if (s < Bias)
        {
            // Overprediction
            int k = (int)(Bias - 1 - s);
            uint d = (1u << k) + _decoder.Decode(k);
            return predicted - d;
        }
        else
        {
            // Perfect prediction - return predicted as-is (identity for lossless)
            return predicted;
        }
    }
}

/// <summary>
/// Predictive coder decoder for double values (64-bit).
/// Uses wide decoding mode (bits > 8) for lossless decompression.
/// </summary>
public sealed class PCDecoderDouble
{
    /// <summary>
    /// Number of symbols in the probability model (2 * 64 + 1 = 129).
    /// </summary>
    public const int Symbols = 2 * 64 + 1;

    private const uint Bias = 64; // perfect prediction symbol

    private readonly RangeDecoder _decoder;
    private readonly RCQsModel _model;

    public PCDecoderDouble(RangeDecoder decoder, RCQsModel model)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Decodes a value given a prediction.
    /// </summary>
    /// <param name="predicted">The predicted mapped value</param>
    /// <returns>The actual mapped value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Decode(ulong predicted)
    {
        uint s = _decoder.Decode(_model);

        if (s > Bias)
        {
            // Underprediction
            int k = (int)(s - Bias - 1);
            ulong d = (1ul << k) + _decoder.DecodeLong(k);
            return predicted + d;
        }
        else if (s < Bias)
        {
            // Overprediction
            int k = (int)(Bias - 1 - s);
            ulong d = (1ul << k) + _decoder.DecodeLong(k);
            return predicted - d;
        }
        else
        {
            // Perfect prediction - return predicted as-is (identity for lossless)
            return predicted;
        }
    }
}
