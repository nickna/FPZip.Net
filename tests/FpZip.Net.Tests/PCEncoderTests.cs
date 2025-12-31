using Xunit;
using FpZip.Core;
using FpZip.Coding;

namespace FpZip.Tests;

public class PCEncoderTests
{
    #region Float Tests

    [Fact]
    public void Float_PerfectPrediction_RoundTrip()
    {
        // When actual equals predicted, should encode efficiently
        uint[] testValues = { 0, 1, 100, 0x80000000, 0xFFFFFFFF };

        foreach (uint value in testValues)
        {
            using var stream = new MemoryStream();
            var encodeModel = new RCQsModel(compress: true, PCEncoderFloat.Symbols);

            using (var rangeEncoder = new RangeEncoder(stream))
            {
                var pcEncoder = new PCEncoderFloat(rangeEncoder, encodeModel);
                pcEncoder.Encode(actual: value, predicted: value);
                rangeEncoder.Finish();
            }

            stream.Position = 0;
            var decodeModel = new RCQsModel(compress: false, PCDecoderFloat.Symbols);

            using var rangeDecoder = new RangeDecoder(stream);
            rangeDecoder.Init();
            var pcDecoder = new PCDecoderFloat(rangeDecoder, decodeModel);

            uint decoded = pcDecoder.Decode(predicted: value);
            Assert.Equal(value, decoded);
        }
    }

    [Fact]
    public void Float_Underprediction_RoundTrip()
    {
        // actual > predicted (underprediction)
        var testCases = new (uint actual, uint predicted)[]
        {
            (10, 5),
            (100, 0),
            (0xFFFFFFFF, 0),
            (1000, 999),
        };

        foreach (var (actual, predicted) in testCases)
        {
            using var stream = new MemoryStream();
            var encodeModel = new RCQsModel(compress: true, PCEncoderFloat.Symbols);

            using (var rangeEncoder = new RangeEncoder(stream))
            {
                var pcEncoder = new PCEncoderFloat(rangeEncoder, encodeModel);
                pcEncoder.Encode(actual, predicted);
                rangeEncoder.Finish();
            }

            stream.Position = 0;
            var decodeModel = new RCQsModel(compress: false, PCDecoderFloat.Symbols);

            using var rangeDecoder = new RangeDecoder(stream);
            rangeDecoder.Init();
            var pcDecoder = new PCDecoderFloat(rangeDecoder, decodeModel);

            uint decoded = pcDecoder.Decode(predicted);
            Assert.Equal(actual, decoded);
        }
    }

    [Fact]
    public void Float_Overprediction_RoundTrip()
    {
        // actual < predicted (overprediction)
        var testCases = new (uint actual, uint predicted)[]
        {
            (5, 10),
            (0, 100),
            (0, 0xFFFFFFFF),
            (999, 1000),
        };

        foreach (var (actual, predicted) in testCases)
        {
            using var stream = new MemoryStream();
            var encodeModel = new RCQsModel(compress: true, PCEncoderFloat.Symbols);

            using (var rangeEncoder = new RangeEncoder(stream))
            {
                var pcEncoder = new PCEncoderFloat(rangeEncoder, encodeModel);
                pcEncoder.Encode(actual, predicted);
                rangeEncoder.Finish();
            }

            stream.Position = 0;
            var decodeModel = new RCQsModel(compress: false, PCDecoderFloat.Symbols);

            using var rangeDecoder = new RangeDecoder(stream);
            rangeDecoder.Init();
            var pcDecoder = new PCDecoderFloat(rangeDecoder, decodeModel);

            uint decoded = pcDecoder.Decode(predicted);
            Assert.Equal(actual, decoded);
        }
    }

    [Fact]
    public void Float_SmallDeltas_RoundTrip()
    {
        // Test small deltas (1-8 bits)
        uint predicted = 1000;
        uint[] deltas = { 1, 2, 3, 7, 15, 31, 63, 127, 255 };

        foreach (uint delta in deltas)
        {
            // Test positive delta (underprediction)
            TestFloatRoundTrip(predicted + delta, predicted);

            // Test negative delta (overprediction) if it doesn't underflow
            if (predicted >= delta)
            {
                TestFloatRoundTrip(predicted - delta, predicted);
            }
        }
    }

    [Fact]
    public void Float_LargeDeltas_RoundTrip()
    {
        // Test large deltas (24-31 bits)
        uint predicted = 0x80000000;
        uint[] deltas = { 0x1000000, 0x10000000, 0x40000000, 0x7FFFFFFF };

        foreach (uint delta in deltas)
        {
            // Test positive delta
            if (predicted <= uint.MaxValue - delta)
            {
                TestFloatRoundTrip(predicted + delta, predicted);
            }

            // Test negative delta
            if (predicted >= delta)
            {
                TestFloatRoundTrip(predicted - delta, predicted);
            }
        }
    }

    [Fact]
    public void Float_AllDeltaSizes_RoundTrip()
    {
        // Test one delta of each bit size (1 through 31)
        uint predicted = 0x80000000;

        for (int bits = 1; bits <= 31; bits++)
        {
            uint delta = 1u << (bits - 1); // Smallest number requiring 'bits' bits

            // Test underprediction
            if (predicted <= uint.MaxValue - delta)
            {
                TestFloatRoundTrip(predicted + delta, predicted);
            }

            // Test overprediction
            if (predicted >= delta)
            {
                TestFloatRoundTrip(predicted - delta, predicted);
            }
        }
    }

    [Fact]
    public void Float_SequenceOfValues_RoundTrip()
    {
        // Test encoding a sequence of values
        uint[] actuals = { 100, 105, 110, 108, 115, 120, 119, 125 };
        uint[] predictions = { 0, 100, 105, 110, 108, 115, 120, 119 };

        using var stream = new MemoryStream();
        var encodeModel = new RCQsModel(compress: true, PCEncoderFloat.Symbols);

        using (var rangeEncoder = new RangeEncoder(stream))
        {
            var pcEncoder = new PCEncoderFloat(rangeEncoder, encodeModel);
            for (int i = 0; i < actuals.Length; i++)
            {
                pcEncoder.Encode(actuals[i], predictions[i]);
            }
            rangeEncoder.Finish();
        }

        stream.Position = 0;
        var decodeModel = new RCQsModel(compress: false, PCDecoderFloat.Symbols);

        using var rangeDecoder = new RangeDecoder(stream);
        rangeDecoder.Init();
        var pcDecoder = new PCDecoderFloat(rangeDecoder, decodeModel);

        for (int i = 0; i < actuals.Length; i++)
        {
            uint decoded = pcDecoder.Decode(predictions[i]);
            Assert.Equal(actuals[i], decoded);
        }
    }

    #endregion

    #region Double Tests

    [Fact]
    public void Double_PerfectPrediction_RoundTrip()
    {
        ulong[] testValues = { 0, 1, 100, 0x8000000000000000, 0xFFFFFFFFFFFFFFFF };

        foreach (ulong value in testValues)
        {
            using var stream = new MemoryStream();
            var encodeModel = new RCQsModel(compress: true, PCEncoderDouble.Symbols);

            using (var rangeEncoder = new RangeEncoder(stream))
            {
                var pcEncoder = new PCEncoderDouble(rangeEncoder, encodeModel);
                pcEncoder.Encode(actual: value, predicted: value);
                rangeEncoder.Finish();
            }

            stream.Position = 0;
            var decodeModel = new RCQsModel(compress: false, PCDecoderDouble.Symbols);

            using var rangeDecoder = new RangeDecoder(stream);
            rangeDecoder.Init();
            var pcDecoder = new PCDecoderDouble(rangeDecoder, decodeModel);

            ulong decoded = pcDecoder.Decode(predicted: value);
            Assert.Equal(value, decoded);
        }
    }

    [Fact]
    public void Double_Underprediction_RoundTrip()
    {
        var testCases = new (ulong actual, ulong predicted)[]
        {
            (10, 5),
            (100, 0),
            (0xFFFFFFFFFFFFFFFF, 0),
            (1000, 999),
        };

        foreach (var (actual, predicted) in testCases)
        {
            using var stream = new MemoryStream();
            var encodeModel = new RCQsModel(compress: true, PCEncoderDouble.Symbols);

            using (var rangeEncoder = new RangeEncoder(stream))
            {
                var pcEncoder = new PCEncoderDouble(rangeEncoder, encodeModel);
                pcEncoder.Encode(actual, predicted);
                rangeEncoder.Finish();
            }

            stream.Position = 0;
            var decodeModel = new RCQsModel(compress: false, PCDecoderDouble.Symbols);

            using var rangeDecoder = new RangeDecoder(stream);
            rangeDecoder.Init();
            var pcDecoder = new PCDecoderDouble(rangeDecoder, decodeModel);

            ulong decoded = pcDecoder.Decode(predicted);
            Assert.Equal(actual, decoded);
        }
    }

    [Fact]
    public void Double_Overprediction_RoundTrip()
    {
        var testCases = new (ulong actual, ulong predicted)[]
        {
            (5, 10),
            (0, 100),
            (0, 0xFFFFFFFFFFFFFFFF),
            (999, 1000),
        };

        foreach (var (actual, predicted) in testCases)
        {
            using var stream = new MemoryStream();
            var encodeModel = new RCQsModel(compress: true, PCEncoderDouble.Symbols);

            using (var rangeEncoder = new RangeEncoder(stream))
            {
                var pcEncoder = new PCEncoderDouble(rangeEncoder, encodeModel);
                pcEncoder.Encode(actual, predicted);
                rangeEncoder.Finish();
            }

            stream.Position = 0;
            var decodeModel = new RCQsModel(compress: false, PCDecoderDouble.Symbols);

            using var rangeDecoder = new RangeDecoder(stream);
            rangeDecoder.Init();
            var pcDecoder = new PCDecoderDouble(rangeDecoder, decodeModel);

            ulong decoded = pcDecoder.Decode(predicted);
            Assert.Equal(actual, decoded);
        }
    }

    [Fact]
    public void Double_AllDeltaSizes_RoundTrip()
    {
        // Test one delta of each bit size (1 through 63)
        ulong predicted = 0x8000000000000000;

        for (int bits = 1; bits <= 63; bits++)
        {
            ulong delta = 1ul << (bits - 1);

            // Test underprediction
            if (predicted <= ulong.MaxValue - delta)
            {
                TestDoubleRoundTrip(predicted + delta, predicted);
            }

            // Test overprediction
            if (predicted >= delta)
            {
                TestDoubleRoundTrip(predicted - delta, predicted);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static void TestFloatRoundTrip(uint actual, uint predicted)
    {
        using var stream = new MemoryStream();
        var encodeModel = new RCQsModel(compress: true, PCEncoderFloat.Symbols);

        using (var rangeEncoder = new RangeEncoder(stream))
        {
            var pcEncoder = new PCEncoderFloat(rangeEncoder, encodeModel);
            pcEncoder.Encode(actual, predicted);
            rangeEncoder.Finish();
        }

        stream.Position = 0;
        var decodeModel = new RCQsModel(compress: false, PCDecoderFloat.Symbols);

        using var rangeDecoder = new RangeDecoder(stream);
        rangeDecoder.Init();
        var pcDecoder = new PCDecoderFloat(rangeDecoder, decodeModel);

        uint decoded = pcDecoder.Decode(predicted);
        Assert.Equal(actual, decoded);
    }

    private static void TestDoubleRoundTrip(ulong actual, ulong predicted)
    {
        using var stream = new MemoryStream();
        var encodeModel = new RCQsModel(compress: true, PCEncoderDouble.Symbols);

        using (var rangeEncoder = new RangeEncoder(stream))
        {
            var pcEncoder = new PCEncoderDouble(rangeEncoder, encodeModel);
            pcEncoder.Encode(actual, predicted);
            rangeEncoder.Finish();
        }

        stream.Position = 0;
        var decodeModel = new RCQsModel(compress: false, PCDecoderDouble.Symbols);

        using var rangeDecoder = new RangeDecoder(stream);
        rangeDecoder.Init();
        var pcDecoder = new PCDecoderDouble(rangeDecoder, decodeModel);

        ulong decoded = pcDecoder.Decode(predicted);
        Assert.Equal(actual, decoded);
    }

    #endregion
}
