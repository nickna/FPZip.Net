using System.Runtime.InteropServices;
using FpZip.Coding;

namespace FpZip;

/// <summary>
/// FpZip compression for floating-point arrays.
/// Provides lossless compression for multi-dimensional float and double arrays.
/// </summary>
public static class FpZipCompressor
{
    /// <summary>
    /// Compresses a float array.
    /// </summary>
    /// <param name="data">The array to compress</param>
    /// <param name="nx">X dimension size</param>
    /// <param name="ny">Y dimension size (default 1)</param>
    /// <param name="nz">Z dimension size (default 1)</param>
    /// <param name="nf">Number of fields (default 1)</param>
    /// <returns>Compressed data as byte array</returns>
    public static byte[] Compress(float[] data, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        return Compress(data.AsSpan(), nx, ny, nz, nf);
    }

    /// <summary>
    /// Compresses a float span.
    /// </summary>
    public static byte[] Compress(ReadOnlySpan<float> data, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        ValidateDimensions(data.Length, nx, ny, nz, nf);

        using var output = new MemoryStream();
        var header = new FpZipHeader(FpZipType.Float, nx, ny, nz, nf);
        header.WriteTo(output);

        using var encoder = new RangeEncoder(output);
        FpZipEncoder.Compress4DFloat(encoder, data, nx, ny, nz, nf);
        encoder.Finish();

        return output.ToArray();
    }

    /// <summary>
    /// Compresses a double array.
    /// </summary>
    public static byte[] Compress(double[] data, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        return Compress(data.AsSpan(), nx, ny, nz, nf);
    }

    /// <summary>
    /// Compresses a double span.
    /// </summary>
    public static byte[] Compress(ReadOnlySpan<double> data, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        ValidateDimensions(data.Length, nx, ny, nz, nf);

        using var output = new MemoryStream();
        var header = new FpZipHeader(FpZipType.Double, nx, ny, nz, nf);
        header.WriteTo(output);

        using var encoder = new RangeEncoder(output);
        FpZipEncoder.Compress4DDouble(encoder, data, nx, ny, nz, nf);
        encoder.Finish();

        return output.ToArray();
    }

    /// <summary>
    /// Compresses a float array to a stream.
    /// </summary>
    public static void Compress(ReadOnlySpan<float> data, Stream output, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        ValidateDimensions(data.Length, nx, ny, nz, nf);

        var header = new FpZipHeader(FpZipType.Float, nx, ny, nz, nf);
        header.WriteTo(output);

        using var encoder = new RangeEncoder(output);
        FpZipEncoder.Compress4DFloat(encoder, data, nx, ny, nz, nf);
        encoder.Finish();
    }

    /// <summary>
    /// Compresses a double array to a stream.
    /// </summary>
    public static void Compress(ReadOnlySpan<double> data, Stream output, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        ValidateDimensions(data.Length, nx, ny, nz, nf);

        var header = new FpZipHeader(FpZipType.Double, nx, ny, nz, nf);
        header.WriteTo(output);

        using var encoder = new RangeEncoder(output);
        FpZipEncoder.Compress4DDouble(encoder, data, nx, ny, nz, nf);
        encoder.Finish();
    }

    /// <summary>
    /// Decompresses data to a float array.
    /// </summary>
    public static float[] DecompressFloat(byte[] data)
    {
        var header = FpZipHeader.ReadFrom(data);
        if (header.Type != FpZipType.Float)
            throw new InvalidDataException($"Expected float data but got {header.Type}");

        var result = new float[header.TotalElements];
        // Use MemoryStream with offset to avoid copying the compressed data
        using var input = new MemoryStream(data, FpZipHeader.HeaderSize, data.Length - FpZipHeader.HeaderSize, writable: false);
        using var decoder = new RangeDecoder(input);
        decoder.Init();

        FpZipDecoder.Decompress4DFloat(decoder, result, header.Nx, header.Ny, header.Nz, header.Nf);
        return result;
    }

    /// <summary>
    /// Decompresses data to a float array.
    /// </summary>
    public static float[] DecompressFloat(ReadOnlySpan<byte> data)
    {
        var header = FpZipHeader.ReadFrom(data);
        if (header.Type != FpZipType.Float)
            throw new InvalidDataException($"Expected float data but got {header.Type}");

        var result = new float[header.TotalElements];
        // For span input, we must copy to create MemoryStream (unavoidable)
        using var input = new MemoryStream(data.Slice(FpZipHeader.HeaderSize).ToArray());
        using var decoder = new RangeDecoder(input);
        decoder.Init();

        FpZipDecoder.Decompress4DFloat(decoder, result, header.Nx, header.Ny, header.Nz, header.Nf);
        return result;
    }

    /// <summary>
    /// Decompresses data to a double array.
    /// </summary>
    public static double[] DecompressDouble(byte[] data)
    {
        var header = FpZipHeader.ReadFrom(data);
        if (header.Type != FpZipType.Double)
            throw new InvalidDataException($"Expected double data but got {header.Type}");

        var result = new double[header.TotalElements];
        // Use MemoryStream with offset to avoid copying the compressed data
        using var input = new MemoryStream(data, FpZipHeader.HeaderSize, data.Length - FpZipHeader.HeaderSize, writable: false);
        using var decoder = new RangeDecoder(input);
        decoder.Init();

        FpZipDecoder.Decompress4DDouble(decoder, result, header.Nx, header.Ny, header.Nz, header.Nf);
        return result;
    }

    /// <summary>
    /// Decompresses data to a double array.
    /// </summary>
    public static double[] DecompressDouble(ReadOnlySpan<byte> data)
    {
        var header = FpZipHeader.ReadFrom(data);
        if (header.Type != FpZipType.Double)
            throw new InvalidDataException($"Expected double data but got {header.Type}");

        var result = new double[header.TotalElements];
        // For span input, we must copy to create MemoryStream (unavoidable)
        using var input = new MemoryStream(data.Slice(FpZipHeader.HeaderSize).ToArray());
        using var decoder = new RangeDecoder(input);
        decoder.Init();

        FpZipDecoder.Decompress4DDouble(decoder, result, header.Nx, header.Ny, header.Nz, header.Nf);
        return result;
    }

    /// <summary>
    /// Decompresses from a stream. Returns the header with dimensions.
    /// </summary>
    public static FpZipHeader Decompress(Stream input, Span<float> output)
    {
        var header = FpZipHeader.ReadFrom(input);
        if (header.Type != FpZipType.Float)
            throw new InvalidDataException($"Expected float data but got {header.Type}");

        if (output.Length < header.TotalElements)
            throw new ArgumentException($"Output buffer too small. Need {header.TotalElements} elements.", nameof(output));

        using var decoder = new RangeDecoder(input);
        decoder.Init();

        FpZipDecoder.Decompress4DFloat(decoder, output, header.Nx, header.Ny, header.Nz, header.Nf);
        return header;
    }

    /// <summary>
    /// Decompresses from a stream. Returns the header with dimensions.
    /// </summary>
    public static FpZipHeader Decompress(Stream input, Span<double> output)
    {
        var header = FpZipHeader.ReadFrom(input);
        if (header.Type != FpZipType.Double)
            throw new InvalidDataException($"Expected double data but got {header.Type}");

        if (output.Length < header.TotalElements)
            throw new ArgumentException($"Output buffer too small. Need {header.TotalElements} elements.", nameof(output));

        using var decoder = new RangeDecoder(input);
        decoder.Init();

        FpZipDecoder.Decompress4DDouble(decoder, output, header.Nx, header.Ny, header.Nz, header.Nf);
        return header;
    }

    /// <summary>
    /// Reads the header from compressed data without decompressing.
    /// </summary>
    public static FpZipHeader ReadHeader(ReadOnlySpan<byte> data)
    {
        return FpZipHeader.ReadFrom(data);
    }

    /// <summary>
    /// Reads the header from a stream without decompressing.
    /// </summary>
    public static FpZipHeader ReadHeader(Stream input)
    {
        return FpZipHeader.ReadFrom(input);
    }

    /// <summary>
    /// Gets the maximum possible compressed size for the given element count.
    /// Use this to allocate a buffer for zero-allocation compression.
    /// </summary>
    /// <param name="elementCount">Number of float or double elements</param>
    /// <param name="type">The data type (Float or Double)</param>
    /// <returns>Maximum possible compressed size in bytes</returns>
    public static int GetMaxCompressedSize(int elementCount, FpZipType type)
    {
        // Header + worst case: each element expands slightly due to entropy coding overhead
        // In practice, FpZip almost always compresses, but worst case is ~1.05x original size
        int elementSize = type == FpZipType.Float ? sizeof(float) : sizeof(double);
        int dataSize = elementCount * elementSize;
        // Add 5% overhead margin + header size
        return FpZipHeader.HeaderSize + dataSize + (dataSize / 20) + 64;
    }

    /// <summary>
    /// Compresses a float array to a pre-allocated buffer.
    /// </summary>
    /// <param name="data">The array to compress</param>
    /// <param name="destination">The destination buffer (use GetMaxCompressedSize to determine size)</param>
    /// <param name="nx">X dimension size</param>
    /// <param name="ny">Y dimension size (default 1)</param>
    /// <param name="nz">Z dimension size (default 1)</param>
    /// <param name="nf">Number of fields (default 1)</param>
    /// <returns>Number of bytes written, or -1 if destination is too small</returns>
    public static int Compress(ReadOnlySpan<float> data, Span<byte> destination, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        ValidateDimensions(data.Length, nx, ny, nz, nf);

        if (destination.Length < FpZipHeader.HeaderSize)
            return -1;

        var header = new FpZipHeader(FpZipType.Float, nx, ny, nz, nf);

        // Use a MemoryStream backed by a rented array for encoding
        using var output = new MemoryStream();
        header.WriteTo(output);

        using var encoder = new RangeEncoder(output);
        FpZipEncoder.Compress4DFloat(encoder, data, nx, ny, nz, nf);
        encoder.Finish();

        int totalBytes = (int)output.Length;
        if (totalBytes > destination.Length)
            return -1;

        output.Position = 0;
        output.Read(destination.Slice(0, totalBytes));
        return totalBytes;
    }

    /// <summary>
    /// Compresses a double array to a pre-allocated buffer.
    /// </summary>
    /// <param name="data">The array to compress</param>
    /// <param name="destination">The destination buffer (use GetMaxCompressedSize to determine size)</param>
    /// <param name="nx">X dimension size</param>
    /// <param name="ny">Y dimension size (default 1)</param>
    /// <param name="nz">Z dimension size (default 1)</param>
    /// <param name="nf">Number of fields (default 1)</param>
    /// <returns>Number of bytes written, or -1 if destination is too small</returns>
    public static int Compress(ReadOnlySpan<double> data, Span<byte> destination, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        ValidateDimensions(data.Length, nx, ny, nz, nf);

        if (destination.Length < FpZipHeader.HeaderSize)
            return -1;

        var header = new FpZipHeader(FpZipType.Double, nx, ny, nz, nf);

        // Use a MemoryStream backed by a rented array for encoding
        using var output = new MemoryStream();
        header.WriteTo(output);

        using var encoder = new RangeEncoder(output);
        FpZipEncoder.Compress4DDouble(encoder, data, nx, ny, nz, nf);
        encoder.Finish();

        int totalBytes = (int)output.Length;
        if (totalBytes > destination.Length)
            return -1;

        output.Position = 0;
        output.Read(destination.Slice(0, totalBytes));
        return totalBytes;
    }

    /// <summary>
    /// Decompresses data to a pre-allocated float span.
    /// </summary>
    /// <param name="data">The compressed data</param>
    /// <param name="output">The destination buffer for decompressed floats</param>
    /// <returns>The header containing dimension information</returns>
    public static FpZipHeader DecompressFloat(ReadOnlySpan<byte> data, Span<float> output)
    {
        var header = FpZipHeader.ReadFrom(data);
        if (header.Type != FpZipType.Float)
            throw new InvalidDataException($"Expected float data but got {header.Type}");

        if (output.Length < header.TotalElements)
            throw new ArgumentException($"Output buffer too small. Need {header.TotalElements} elements.", nameof(output));

        using var input = new MemoryStream(data.Slice(FpZipHeader.HeaderSize).ToArray());
        using var decoder = new RangeDecoder(input);
        decoder.Init();

        FpZipDecoder.Decompress4DFloat(decoder, output, header.Nx, header.Ny, header.Nz, header.Nf);
        return header;
    }

    /// <summary>
    /// Decompresses data to a pre-allocated double span.
    /// </summary>
    /// <param name="data">The compressed data</param>
    /// <param name="output">The destination buffer for decompressed doubles</param>
    /// <returns>The header containing dimension information</returns>
    public static FpZipHeader DecompressDouble(ReadOnlySpan<byte> data, Span<double> output)
    {
        var header = FpZipHeader.ReadFrom(data);
        if (header.Type != FpZipType.Double)
            throw new InvalidDataException($"Expected double data but got {header.Type}");

        if (output.Length < header.TotalElements)
            throw new ArgumentException($"Output buffer too small. Need {header.TotalElements} elements.", nameof(output));

        using var input = new MemoryStream(data.Slice(FpZipHeader.HeaderSize).ToArray());
        using var decoder = new RangeDecoder(input);
        decoder.Init();

        FpZipDecoder.Decompress4DDouble(decoder, output, header.Nx, header.Ny, header.Nz, header.Nf);
        return header;
    }

    private static void ValidateDimensions(int dataLength, int nx, int ny, int nz, int nf)
    {
        long expectedLength = (long)nx * ny * nz * nf;
        if (dataLength != expectedLength)
            throw new ArgumentException($"Data length {dataLength} does not match dimensions ({nx} x {ny} x {nz} x {nf} = {expectedLength})");
    }
}
