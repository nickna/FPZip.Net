namespace FpZip.Tests.TestHelpers;

/// <summary>
/// Helper methods for compression ratio testing.
/// </summary>
public static class CompressionTestHelper
{
    /// <summary>
    /// Compresses float data and returns compression statistics.
    /// </summary>
    public static CompressionStats CompressFloat(
        float[] data, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        int originalBytes = data.Length * sizeof(float);
        byte[] compressed = FpZipCompressor.Compress(data, nx, ny, nz, nf);

        return new CompressionStats
        {
            OriginalBytes = originalBytes,
            CompressedBytes = compressed.Length,
            ValueCount = data.Length,
            CompressedData = compressed
        };
    }

    /// <summary>
    /// Compresses double data and returns compression statistics.
    /// </summary>
    public static CompressionStats CompressDouble(
        double[] data, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        int originalBytes = data.Length * sizeof(double);
        byte[] compressed = FpZipCompressor.Compress(data, nx, ny, nz, nf);

        return new CompressionStats
        {
            OriginalBytes = originalBytes,
            CompressedBytes = compressed.Length,
            ValueCount = data.Length,
            CompressedData = compressed
        };
    }

    /// <summary>
    /// Computes Jenkins one-at-a-time hash (same as C++ reference).
    /// Used for cross-validation with C++ implementation.
    /// </summary>
    public static uint JenkinsHash(ReadOnlySpan<byte> data)
    {
        uint h = 0;
        foreach (byte b in data)
        {
            h += b;
            h += h << 10;
            h ^= h >> 6;
        }
        h += h << 3;
        h ^= h >> 11;
        h += h << 15;
        return h;
    }
}

/// <summary>
/// Statistics from a compression operation.
/// </summary>
public readonly struct CompressionStats
{
    public required int OriginalBytes { get; init; }
    public required int CompressedBytes { get; init; }
    public required int ValueCount { get; init; }
    public required byte[] CompressedData { get; init; }

    /// <summary>
    /// Compression ratio (original size / compressed size).
    /// Higher values indicate better compression.
    /// </summary>
    public double CompressionRatio =>
        CompressedBytes == 0 ? double.PositiveInfinity : (double)OriginalBytes / CompressedBytes;

    /// <summary>
    /// Bits per value (compressed bits / number of values).
    /// </summary>
    public double BitsPerValue =>
        ValueCount == 0 ? 0 : (CompressedBytes * 8.0) / ValueCount;
}
