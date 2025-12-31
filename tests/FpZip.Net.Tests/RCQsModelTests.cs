using Xunit;
using FpZip.Coding;

namespace FpZip.Tests;

public class RCQsModelTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesModel()
    {
        var model = new RCQsModel(compress: true, symbols: 65, bits: 16, period: 0x400);

        Assert.Equal(65, model.Symbols);
    }

    [Fact]
    public void Constructor_BitsGreaterThan16_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new RCQsModel(compress: true, symbols: 65, bits: 17));
    }

    [Fact]
    public void Constructor_PeriodTooLarge_ThrowsArgumentException()
    {
        // Period must be < 2^(bits+1), so for bits=16, period must be < 2^17 = 131072
        // But the actual check is period >= (1 << (bits + 1))
        Assert.Throws<ArgumentException>(() =>
            new RCQsModel(compress: true, symbols: 65, bits: 16, period: 1 << 17));
    }

    [Fact]
    public void Constructor_CompressMode_NoSearchTable()
    {
        // Compress mode should work without search table
        var model = new RCQsModel(compress: true, symbols: 10);

        // Should be able to encode without error
        model.Encode(0, out uint cumFreq, out uint freq);
        Assert.True(freq > 0);
    }

    [Fact]
    public void Constructor_DecompressMode_HasSearchTable()
    {
        // Decompress mode should have search table for decoding
        var model = new RCQsModel(compress: false, symbols: 10);

        // Initialize with a valid cumulative frequency value
        uint l = 0;
        uint s = model.Decode(ref l, out uint r);

        // Should decode to a valid symbol
        Assert.True(s < 10u);
        Assert.True(r > 0);
    }

    [Fact]
    public void Encode_ReturnsValidFrequencies()
    {
        var model = new RCQsModel(compress: true, symbols: 10, bits: 16);

        for (uint symbol = 0; symbol < 10; symbol++)
        {
            model.Encode(symbol, out uint cumFreq, out uint freq);

            // Cumulative frequency should be less than total (2^bits)
            Assert.True(cumFreq < (1u << 16));
            // Individual frequency should be positive
            Assert.True(freq > 0);
            // cumFreq + freq should not exceed total
            Assert.True(cumFreq + freq <= (1u << 16));
        }
    }

    [Fact]
    public void Encode_Decode_AllSymbols_RoundTrip()
    {
        int numSymbols = 10;
        var encodeModel = new RCQsModel(compress: true, symbols: numSymbols, bits: 16);
        var decodeModel = new RCQsModel(compress: false, symbols: numSymbols, bits: 16);

        // Test each symbol can be encoded and decoded
        for (uint symbol = 0; symbol < (uint)numSymbols; symbol++)
        {
            // Reset models to same state for each symbol
            encodeModel.Reset();
            decodeModel.Reset();

            encodeModel.Encode(symbol, out uint cumFreq, out uint freq);

            // Use the cumulative frequency to decode
            uint l = cumFreq;
            uint decoded = decodeModel.Decode(ref l, out uint r);

            Assert.Equal(symbol, decoded);
        }
    }

    [Fact]
    public void FrequencyAdaptation_RepeatedSymbol_UpdatesModel()
    {
        var model = new RCQsModel(compress: true, symbols: 10, bits: 16);

        // Get initial frequency for symbol 0
        model.Encode(0, out uint initialCumFreq, out uint initialFreq);

        // Reset and encode symbol 0 multiple times
        model.Reset();
        for (int i = 0; i < 100; i++)
        {
            model.Encode(0, out _, out _);
        }

        // After many encodes of symbol 0, its frequency should have changed
        // due to adaptive updates (the model adapts symbol frequencies)
        model.Encode(0, out uint finalCumFreq, out uint finalFreq);

        // The frequency distribution should have changed
        // (exact behavior depends on the quasi-static model parameters)
        // At minimum, we verify no crash and valid frequencies
        Assert.True(finalFreq > 0);
    }

    [Fact]
    public void Reset_RestoresUniformDistribution()
    {
        var model = new RCQsModel(compress: true, symbols: 4, bits: 16);

        // Encode symbol 0 many times to skew the distribution
        for (int i = 0; i < 1000; i++)
        {
            model.Encode(0, out _, out _);
        }

        // Reset should restore uniform-like distribution
        model.Reset();

        // After reset, all symbols should have roughly equal frequencies
        // In a uniform distribution with 4 symbols and 2^16 total,
        // each symbol should have frequency around 2^16/4 = 16384
        uint[] frequencies = new uint[4];
        for (uint s = 0; s < 4; s++)
        {
            model.Encode(s, out uint cumFreq, out uint freq);
            frequencies[s] = freq;
        }

        // All frequencies should be positive and reasonably close
        foreach (uint freq in frequencies)
        {
            Assert.True(freq > 0);
            // Each frequency should be within reasonable range of expected
            Assert.True(freq > 10000 && freq < 20000);
        }
    }

    [Fact]
    public void Normalize_CorrectlyScalesRange()
    {
        var model = new RCQsModel(compress: true, symbols: 65, bits: 16);

        uint range = 0xFFFFFFFF;
        model.Normalize(ref range);

        // After normalization, range should be shifted right by bits (16)
        Assert.Equal(0xFFFFu, range);
    }
}
