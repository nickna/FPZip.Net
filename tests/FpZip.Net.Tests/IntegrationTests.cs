using Xunit;
using System.Runtime.InteropServices;

namespace FpZip.Tests;

public class IntegrationTests
{
    [Fact]
    public void Float_SimpleArray_RoundTrip()
    {
        float[] original = { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f };

        byte[] compressed = FpZipCompressor.Compress(original, nx: 8);
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Double_SimpleArray_RoundTrip()
    {
        double[] original = { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0 };

        byte[] compressed = FpZipCompressor.Compress(original, nx: 8);
        double[] decompressed = FpZipCompressor.DecompressDouble(compressed);

        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Float_2DArray_RoundTrip()
    {
        float[] original = Enumerable.Range(0, 16).Select(i => (float)i).ToArray();

        byte[] compressed = FpZipCompressor.Compress(original, nx: 4, ny: 4);
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Float_3DArray_RoundTrip()
    {
        float[] original = Enumerable.Range(0, 64).Select(i => (float)i).ToArray();

        byte[] compressed = FpZipCompressor.Compress(original, nx: 4, ny: 4, nz: 4);
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Float_SmoothGradient_Compresses()
    {
        // Smooth data should compress well
        float[] original = Enumerable.Range(0, 1000).Select(i => i * 0.1f).ToArray();

        byte[] compressed = FpZipCompressor.Compress(original, nx: 10, ny: 10, nz: 10);
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        Assert.Equal(original, decompressed);

        // Just verify it compresses and decompresses correctly
        // Actual compression ratio depends on data characteristics
    }

    [Fact]
    public void Float_RandomData_RoundTrip()
    {
        // Random data - should still round-trip correctly
        var random = new Random(42);
        float[] original = new float[1000];
        for (int i = 0; i < original.Length; i++)
            original[i] = (float)(random.NextDouble() * 1000 - 500);

        byte[] compressed = FpZipCompressor.Compress(original, nx: 10, ny: 10, nz: 10);
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Float_SpecialValues_RoundTrip()
    {
        float[] original = {
            0f, -0f, 1f, -1f,
            float.Epsilon, -float.Epsilon,
            float.MaxValue, float.MinValue,
            float.PositiveInfinity, float.NegativeInfinity,
            float.NaN
        };

        byte[] compressed = FpZipCompressor.Compress(original, nx: original.Length);
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        // Compare element by element (NaN requires special handling)
        Assert.Equal(original.Length, decompressed.Length);
        for (int i = 0; i < original.Length; i++)
        {
            if (float.IsNaN(original[i]))
                Assert.True(float.IsNaN(decompressed[i]));
            else
                Assert.Equal(original[i], decompressed[i]);
        }
    }

    [Fact]
    public void Double_SpecialValues_RoundTrip()
    {
        double[] original = {
            0d, -0d, 1d, -1d,
            double.Epsilon, -double.Epsilon,
            double.MaxValue, double.MinValue,
            double.PositiveInfinity, double.NegativeInfinity,
            double.NaN
        };

        byte[] compressed = FpZipCompressor.Compress(original, nx: original.Length);
        double[] decompressed = FpZipCompressor.DecompressDouble(compressed);

        // Compare element by element (NaN requires special handling)
        Assert.Equal(original.Length, decompressed.Length);
        for (int i = 0; i < original.Length; i++)
        {
            if (double.IsNaN(original[i]))
                Assert.True(double.IsNaN(decompressed[i]));
            else
                Assert.Equal(original[i], decompressed[i]);
        }
    }

    [Fact]
    public void Float_LargeArray_RoundTrip()
    {
        // 100x100x10 = 100,000 elements
        int nx = 100, ny = 100, nz = 10;
        float[] original = new float[nx * ny * nz];

        // Fill with wave pattern (compresses well)
        for (int i = 0; i < original.Length; i++)
            original[i] = MathF.Sin(i * 0.01f) * 100f;

        byte[] compressed = FpZipCompressor.Compress(original, nx, ny, nz);
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Header_ReadsCorrectDimensions()
    {
        float[] data = new float[24]; // 2x3x4
        Array.Fill(data, 1.0f);

        byte[] compressed = FpZipCompressor.Compress(data, nx: 2, ny: 3, nz: 4);
        FpZipHeader header = FpZipCompressor.ReadHeader(compressed);

        Assert.Equal(FpZipType.Float, header.Type);
        Assert.Equal(2, header.Nx);
        Assert.Equal(3, header.Ny);
        Assert.Equal(4, header.Nz);
        Assert.Equal(1, header.Nf);
        Assert.Equal(24, header.TotalElements);
    }

    [Fact]
    public void Float_MultipleFields_RoundTrip()
    {
        // 2 fields of 4x4x4
        int nx = 4, ny = 4, nz = 4, nf = 2;
        float[] original = new float[nx * ny * nz * nf];

        for (int i = 0; i < original.Length; i++)
            original[i] = i * 0.1f;

        byte[] compressed = FpZipCompressor.Compress(original, nx, ny, nz, nf);
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        Assert.Equal(original, decompressed);

        FpZipHeader header = FpZipCompressor.ReadHeader(compressed);
        Assert.Equal(nf, header.Nf);
    }

    [Fact]
    public void Stream_CompressDecompress_Works()
    {
        float[] original = { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f };

        using var outputStream = new MemoryStream();
        FpZipCompressor.Compress(original.AsSpan(), outputStream, nx: 8);

        byte[] compressed = outputStream.ToArray();
        float[] decompressed = FpZipCompressor.DecompressFloat(compressed);

        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void InvalidDimensions_ThrowsException()
    {
        float[] data = new float[10];

        // Data length doesn't match dimensions
        Assert.Throws<ArgumentException>(() =>
            FpZipCompressor.Compress(data, nx: 5, ny: 5)); // 5*5=25 != 10
    }

    [Fact]
    public void WrongType_ThrowsException()
    {
        float[] original = { 1f, 2f, 3f, 4f };
        byte[] compressed = FpZipCompressor.Compress(original, nx: 4);

        // Try to decompress as double
        Assert.Throws<InvalidDataException>(() =>
            FpZipCompressor.DecompressDouble(compressed));
    }
}
