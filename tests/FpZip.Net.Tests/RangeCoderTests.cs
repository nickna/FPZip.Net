using Xunit;
using FpZip.Coding;

namespace FpZip.Tests;

public class RangeCoderTests
{
    [Fact]
    public void EncodeBit_DecodeBit_SingleBit_RoundTrip()
    {
        // Test both true and false bits
        foreach (bool bit in new[] { true, false })
        {
            using var stream = new MemoryStream();

            using (var encoder = new RangeEncoder(stream))
            {
                encoder.EncodeBit(bit);
                encoder.Finish();
            }

            stream.Position = 0;

            using var decoder = new RangeDecoder(stream);
            decoder.Init();
            bool decoded = decoder.DecodeBit();

            Assert.Equal(bit, decoded);
        }
    }

    [Fact]
    public void EncodeBit_DecodeBit_MultipleBits_RoundTrip()
    {
        bool[] bits = { true, false, true, true, false, false, true, false };

        using var stream = new MemoryStream();

        using (var encoder = new RangeEncoder(stream))
        {
            foreach (bool bit in bits)
            {
                encoder.EncodeBit(bit);
            }
            encoder.Finish();
        }

        stream.Position = 0;

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        for (int i = 0; i < bits.Length; i++)
        {
            bool decoded = decoder.DecodeBit();
            Assert.Equal(bits[i], decoded);
        }
    }

    [Fact]
    public void Encode_Decode_WithModel_RoundTrip()
    {
        int numSymbols = 65; // Same as PCEncoderFloat.Symbols
        uint[] testSymbols = { 0, 1, 32, 63, 64 }; // Various symbols including bias

        using var stream = new MemoryStream();

        var encodeModel = new RCQsModel(compress: true, symbols: numSymbols);

        using (var encoder = new RangeEncoder(stream))
        {
            foreach (uint symbol in testSymbols)
            {
                encoder.Encode(symbol, encodeModel);
            }
            encoder.Finish();
        }

        stream.Position = 0;

        var decodeModel = new RCQsModel(compress: false, symbols: numSymbols);

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        foreach (uint expected in testSymbols)
        {
            uint decoded = decoder.Decode(decodeModel);
            Assert.Equal(expected, decoded);
        }
    }

    [Fact]
    public void Encode_Decode_FixedWidth_8Bits_RoundTrip()
    {
        uint[] testValues = { 0, 1, 127, 128, 255 };

        using var stream = new MemoryStream();

        using (var encoder = new RangeEncoder(stream))
        {
            foreach (uint value in testValues)
            {
                encoder.Encode(value, 8);
            }
            encoder.Finish();
        }

        stream.Position = 0;

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        foreach (uint expected in testValues)
        {
            uint decoded = decoder.Decode(8);
            Assert.Equal(expected, decoded);
        }
    }

    [Fact]
    public void Encode_Decode_FixedWidth_16Bits_RoundTrip()
    {
        uint[] testValues = { 0, 1, 256, 32768, 65535 };

        using var stream = new MemoryStream();

        using (var encoder = new RangeEncoder(stream))
        {
            foreach (uint value in testValues)
            {
                encoder.Encode(value, 16);
            }
            encoder.Finish();
        }

        stream.Position = 0;

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        foreach (uint expected in testValues)
        {
            uint decoded = decoder.Decode(16);
            Assert.Equal(expected, decoded);
        }
    }

    [Fact]
    public void Encode_Decode_FixedWidth_32Bits_RoundTrip()
    {
        uint[] testValues = { 0, 1, 0x10000, 0x80000000, 0xFFFFFFFF };

        using var stream = new MemoryStream();

        using (var encoder = new RangeEncoder(stream))
        {
            foreach (uint value in testValues)
            {
                encoder.Encode(value, 32);
            }
            encoder.Finish();
        }

        stream.Position = 0;

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        foreach (uint expected in testValues)
        {
            uint decoded = decoder.Decode(32);
            Assert.Equal(expected, decoded);
        }
    }

    [Fact]
    public void Encode_Decode_64Bit_RoundTrip()
    {
        ulong[] testValues = { 0, 1, 0x100000000, 0x8000000000000000, 0xFFFFFFFFFFFFFFFF };

        using var stream = new MemoryStream();

        using (var encoder = new RangeEncoder(stream))
        {
            foreach (ulong value in testValues)
            {
                encoder.Encode(value, 64);
            }
            encoder.Finish();
        }

        stream.Position = 0;

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        foreach (ulong expected in testValues)
        {
            ulong decoded = decoder.DecodeLong(64);
            Assert.Equal(expected, decoded);
        }
    }

    [Fact]
    public void BytesWritten_TracksCorrectly()
    {
        using var stream = new MemoryStream();

        using (var encoder = new RangeEncoder(stream))
        {
            // Encode some data
            for (int i = 0; i < 100; i++)
            {
                encoder.Encode((uint)i, 8);
            }
            encoder.Finish();

            // BytesWritten should be positive after encoding
            Assert.True(encoder.BytesWritten > 0);
        }

        // Stream length should match what was written
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void BytesRead_TracksCorrectly()
    {
        using var stream = new MemoryStream();

        // First encode some data
        using (var encoder = new RangeEncoder(stream))
        {
            for (int i = 0; i < 10; i++)
            {
                encoder.Encode((uint)i, 8);
            }
            encoder.Finish();
        }

        stream.Position = 0;

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        // BytesRead should be positive after Init (reads 4 bytes)
        Assert.True(decoder.BytesRead > 0);

        // Decode the data
        for (int i = 0; i < 10; i++)
        {
            decoder.Decode(8);
        }

        // BytesRead should have increased
        Assert.True(decoder.BytesRead >= 4); // At least the initial 4 bytes
    }

    [Fact]
    public void Decoder_EOF_SetsErrorFlag()
    {
        // Create a stream with minimal data
        using var stream = new MemoryStream(new byte[] { 0, 0, 0, 0 });

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        // Try to decode more data than available - should eventually hit EOF
        for (int i = 0; i < 1000; i++)
        {
            decoder.Decode(8);
        }

        // After reading past EOF, error flag should be set
        Assert.True(decoder.Error);
    }

    [Fact]
    public void LargeSymbolSequence_RoundTrip()
    {
        int numSymbols = 129; // Same as PCEncoderDouble.Symbols
        int sequenceLength = 1000;

        // Generate a sequence of random symbols
        var random = new Random(42);
        uint[] symbols = new uint[sequenceLength];
        for (int i = 0; i < sequenceLength; i++)
        {
            symbols[i] = (uint)random.Next(numSymbols);
        }

        using var stream = new MemoryStream();

        var encodeModel = new RCQsModel(compress: true, symbols: numSymbols);

        using (var encoder = new RangeEncoder(stream))
        {
            foreach (uint symbol in symbols)
            {
                encoder.Encode(symbol, encodeModel);
            }
            encoder.Finish();
        }

        stream.Position = 0;

        var decodeModel = new RCQsModel(compress: false, symbols: numSymbols);

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        for (int i = 0; i < sequenceLength; i++)
        {
            uint decoded = decoder.Decode(decodeModel);
            Assert.Equal(symbols[i], decoded);
        }
    }

    [Fact]
    public void MixedEncoding_BitsAndSymbols_RoundTrip()
    {
        // Test mixing bit encoding, fixed-width encoding, and model-based encoding
        using var stream = new MemoryStream();

        var model = new RCQsModel(compress: true, symbols: 65);

        using (var encoder = new RangeEncoder(stream))
        {
            encoder.EncodeBit(true);
            encoder.Encode(42u, 8);
            encoder.Encode(32u, model); // Bias symbol
            encoder.EncodeBit(false);
            encoder.Encode(1000u, 16);
            encoder.Finish();
        }

        stream.Position = 0;

        var decodeModel = new RCQsModel(compress: false, symbols: 65);

        using var decoder = new RangeDecoder(stream);
        decoder.Init();

        Assert.True(decoder.DecodeBit());
        Assert.Equal(42u, decoder.Decode(8));
        Assert.Equal(32u, decoder.Decode(decodeModel));
        Assert.False(decoder.DecodeBit());
        Assert.Equal(1000u, decoder.Decode(16));
    }
}
