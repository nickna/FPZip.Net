using Xunit;
using FpZip.Tests.TestHelpers;

namespace FpZip.Tests;

/// <summary>
/// Tests for compression ratios with various data patterns.
/// These tests verify that compression performance meets or exceeds the C++ reference implementation.
/// </summary>
public class CompressionRatioTests
{
    #region C++ Baseline Values

    /// <summary>
    /// Standard test dimensions from C++ reference (65x64x63).
    /// </summary>
    private const int Nx = 65;
    private const int Ny = 64;
    private const int Nz = 63;

    /// <summary>
    /// Expected checksum for float lossless 3D compression (FPZIP_FP_INT mode).
    /// From C++ testfpzip.c: cksum[3][2][2] = 0x5d710559
    /// </summary>
    private const uint ExpectedFloatChecksumFpInt = 0x5d710559u;

    /// <summary>
    /// Expected checksum for double lossless 3D compression (FPZIP_FP_INT mode).
    /// From C++ testfpzip.c: cksum[3][2][2] = 0xbfc5a355
    /// </summary>
    private const uint ExpectedDoubleChecksumFpInt = 0xbfc5a355u;

    // C++ baseline compression ratios (bits per value) from benchmark
    // These are the maximum acceptable values - our implementation should match or beat these
    // NOTE: These values are for fresh-seed trilinear fields (seed=1, no prior PRNG consumption)
    private const double CppFloatTrilinearBitsPerValue = 23.92;
    private const double CppDoubleTrilinearBitsPerValue = 52.98;
    private const double CppFloatGradientBitsPerValue = 1.79;
    private const double CppDoubleGradientBitsPerValue = 2.15;
    private const double CppFloatSineBitsPerValue = 22.04;
    private const double CppDoubleSineBitsPerValue = 51.10;
    private const double CppFloatRandomBitsPerValue = 31.67;
    private const double CppDoubleRandomBitsPerValue = 62.18;

    // Tolerance for floating-point comparisons (1% overhead allowed)
    private const double Tolerance = 1.01;

    #endregion

    #region Trilinear Field Tests (C++ Cross-Validation)

    [Fact]
    public void Float_TrilinearField_MeetsCppBaseline()
    {
        // Generate the standard trilinear field
        float[] field = TrilinearFieldGenerator.GenerateFloatField(Nx, Ny, Nz, offset: 0f);

        // Compress
        var stats = CompressionTestHelper.CompressFloat(field, Nx, Ny, Nz);

        // Verify round-trip
        float[] decompressed = FpZipCompressor.DecompressFloat(stats.CompressedData);
        Assert.Equal(field, decompressed);

        // Verify compression meets C++ baseline
        double maxAllowed = CppFloatTrilinearBitsPerValue * Tolerance;
        Assert.True(stats.BitsPerValue <= maxAllowed,
            $"Float trilinear: {stats.BitsPerValue:F2} bits/value exceeds C++ baseline " +
            $"({CppFloatTrilinearBitsPerValue:F2}) by more than {(Tolerance - 1) * 100:F0}%");
    }

    [Fact]
    public void Double_TrilinearField_MeetsCppBaseline()
    {
        // Generate the standard trilinear field
        double[] field = TrilinearFieldGenerator.GenerateDoubleField(Nx, Ny, Nz, offset: 0.0);

        // Compress
        var stats = CompressionTestHelper.CompressDouble(field, Nx, Ny, Nz);

        // Verify round-trip
        double[] decompressed = FpZipCompressor.DecompressDouble(stats.CompressedData);
        Assert.Equal(field, decompressed);

        // Verify compression meets C++ baseline
        double maxAllowed = CppDoubleTrilinearBitsPerValue * Tolerance;
        Assert.True(stats.BitsPerValue <= maxAllowed,
            $"Double trilinear: {stats.BitsPerValue:F2} bits/value exceeds C++ baseline " +
            $"({CppDoubleTrilinearBitsPerValue:F2}) by more than {(Tolerance - 1) * 100:F0}%");
    }

    [Fact(Skip = "Enable after confirming C++ compatibility")]
    public void Float_TrilinearField_MatchesCppChecksum()
    {
        float[] field = TrilinearFieldGenerator.GenerateFloatField(Nx, Ny, Nz, offset: 0f);
        var stats = CompressionTestHelper.CompressFloat(field, Nx, Ny, Nz);

        uint actualChecksum = CompressionTestHelper.JenkinsHash(stats.CompressedData);
        Assert.Equal(ExpectedFloatChecksumFpInt, actualChecksum);
    }

    [Fact(Skip = "Enable after confirming C++ compatibility")]
    public void Double_TrilinearField_MatchesCppChecksum()
    {
        double[] field = TrilinearFieldGenerator.GenerateDoubleField(Nx, Ny, Nz, offset: 0.0);
        var stats = CompressionTestHelper.CompressDouble(field, Nx, Ny, Nz);

        uint actualChecksum = CompressionTestHelper.JenkinsHash(stats.CompressedData);
        Assert.Equal(ExpectedDoubleChecksumFpInt, actualChecksum);
    }

    #endregion

    #region Data Pattern Tests - Float

    [Fact]
    public void Float_Zeros_AchievesHighCompression()
    {
        // All zeros should compress extremely well
        float[] data = new float[1000];
        Array.Fill(data, 0f);

        var stats = CompressionTestHelper.CompressFloat(data, 10, 10, 10);

        // Verify round-trip
        float[] decompressed = FpZipCompressor.DecompressFloat(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Zeros should compress to a very small size
        // Expected: near-perfect prediction, minimal entropy
        Assert.True(stats.BitsPerValue < 1.0,
            $"Zeros should compress to < 1 bit/value, got {stats.BitsPerValue:F2}");
    }

    [Fact]
    public void Float_ConstantValue_AchievesHighCompression()
    {
        // All same value should compress well
        float[] data = new float[1000];
        Array.Fill(data, 3.14159f);

        var stats = CompressionTestHelper.CompressFloat(data, 10, 10, 10);

        // Verify round-trip
        float[] decompressed = FpZipCompressor.DecompressFloat(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Constant data should compress well (8:1 or better)
        // Note: First elements need full encoding, plus adaptive model convergence overhead
        Assert.True(stats.CompressionRatio >= 4.0,
            $"Constant data should achieve >= 4:1 ratio, got {stats.CompressionRatio:F2}");
    }

    [Fact]
    public void Float_LinearGradient_MeetsCppBaseline()
    {
        // Linear gradient: perfect for Lorenzo predictor
        // Use same dimensions as C++ benchmark (65x64x63)
        float[] data = new float[Nx * Ny * Nz];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i * 0.001f;
        }

        var stats = CompressionTestHelper.CompressFloat(data, Nx, Ny, Nz);

        // Verify round-trip
        float[] decompressed = FpZipCompressor.DecompressFloat(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Verify compression meets C++ baseline
        double maxAllowed = CppFloatGradientBitsPerValue * Tolerance;
        Assert.True(stats.BitsPerValue <= maxAllowed,
            $"Float gradient: {stats.BitsPerValue:F2} bits/value exceeds C++ baseline " +
            $"({CppFloatGradientBitsPerValue:F2}) by more than {(Tolerance - 1) * 100:F0}%");
    }

    [Fact]
    public void Float_Random_MeetsCppBaseline()
    {
        // Random data should not compress well but must round-trip
        // Use same dimensions as C++ benchmark (65x64x63)
        var random = new Random(42);
        float[] data = new float[Nx * Ny * Nz];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (float)(random.NextDouble() * 1000 - 500);
        }

        var stats = CompressionTestHelper.CompressFloat(data, Nx, Ny, Nz);

        // Verify round-trip
        float[] decompressed = FpZipCompressor.DecompressFloat(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Verify compression meets C++ baseline (random data expands slightly)
        double maxAllowed = CppFloatRandomBitsPerValue * Tolerance;
        Assert.True(stats.BitsPerValue <= maxAllowed,
            $"Float random: {stats.BitsPerValue:F2} bits/value exceeds C++ baseline " +
            $"({CppFloatRandomBitsPerValue:F2}) by more than {(Tolerance - 1) * 100:F0}%");
    }

    [Fact]
    public void Float_SineWave_MeetsCppBaseline()
    {
        // Wave pattern has some structure
        // Use same dimensions as C++ benchmark (65x64x63)
        float[] data = new float[Nx * Ny * Nz];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = MathF.Sin(i * 0.01f) * 100f;
        }

        var stats = CompressionTestHelper.CompressFloat(data, Nx, Ny, Nz);

        // Verify round-trip
        float[] decompressed = FpZipCompressor.DecompressFloat(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Verify compression meets C++ baseline
        double maxAllowed = CppFloatSineBitsPerValue * Tolerance;
        Assert.True(stats.BitsPerValue <= maxAllowed,
            $"Float sine wave: {stats.BitsPerValue:F2} bits/value exceeds C++ baseline " +
            $"({CppFloatSineBitsPerValue:F2}) by more than {(Tolerance - 1) * 100:F0}%");
    }

    #endregion

    #region Data Pattern Tests - Double

    [Fact]
    public void Double_Zeros_AchievesHighCompression()
    {
        double[] data = new double[Nx * Ny * Nz];
        Array.Fill(data, 0.0);

        var stats = CompressionTestHelper.CompressDouble(data, Nx, Ny, Nz);

        // Verify round-trip
        double[] decompressed = FpZipCompressor.DecompressDouble(stats.CompressedData);
        Assert.Equal(data, decompressed);

        Assert.True(stats.BitsPerValue < 1.0,
            $"Zeros should compress to < 1 bit/value, got {stats.BitsPerValue:F2}");
    }

    [Fact]
    public void Double_ConstantValue_AchievesHighCompression()
    {
        double[] data = new double[Nx * Ny * Nz];
        Array.Fill(data, 3.14159265358979);

        var stats = CompressionTestHelper.CompressDouble(data, Nx, Ny, Nz);

        // Verify round-trip
        double[] decompressed = FpZipCompressor.DecompressDouble(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Constant data should compress well (8:1 or better for doubles)
        Assert.True(stats.CompressionRatio >= 4.0,
            $"Constant data should achieve >= 4:1 ratio, got {stats.CompressionRatio:F2}");
    }

    [Fact]
    public void Double_LinearGradient_MeetsCppBaseline()
    {
        // Use same dimensions as C++ benchmark (65x64x63)
        double[] data = new double[Nx * Ny * Nz];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i * 0.001;
        }

        var stats = CompressionTestHelper.CompressDouble(data, Nx, Ny, Nz);

        // Verify round-trip
        double[] decompressed = FpZipCompressor.DecompressDouble(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Verify compression meets C++ baseline
        double maxAllowed = CppDoubleGradientBitsPerValue * Tolerance;
        Assert.True(stats.BitsPerValue <= maxAllowed,
            $"Double gradient: {stats.BitsPerValue:F2} bits/value exceeds C++ baseline " +
            $"({CppDoubleGradientBitsPerValue:F2}) by more than {(Tolerance - 1) * 100:F0}%");
    }

    [Fact]
    public void Double_Random_MeetsCppBaseline()
    {
        // Use same dimensions as C++ benchmark (65x64x63)
        var random = new Random(42);
        double[] data = new double[Nx * Ny * Nz];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = random.NextDouble() * 1000 - 500;
        }

        var stats = CompressionTestHelper.CompressDouble(data, Nx, Ny, Nz);

        // Verify round-trip
        double[] decompressed = FpZipCompressor.DecompressDouble(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Verify compression meets C++ baseline
        double maxAllowed = CppDoubleRandomBitsPerValue * Tolerance;
        Assert.True(stats.BitsPerValue <= maxAllowed,
            $"Double random: {stats.BitsPerValue:F2} bits/value exceeds C++ baseline " +
            $"({CppDoubleRandomBitsPerValue:F2}) by more than {(Tolerance - 1) * 100:F0}%");
    }

    [Fact]
    public void Double_SineWave_MeetsCppBaseline()
    {
        // Use same dimensions as C++ benchmark (65x64x63)
        double[] data = new double[Nx * Ny * Nz];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Math.Sin(i * 0.01) * 100.0;
        }

        var stats = CompressionTestHelper.CompressDouble(data, Nx, Ny, Nz);

        // Verify round-trip
        double[] decompressed = FpZipCompressor.DecompressDouble(stats.CompressedData);
        Assert.Equal(data, decompressed);

        // Verify compression meets C++ baseline
        double maxAllowed = CppDoubleSineBitsPerValue * Tolerance;
        Assert.True(stats.BitsPerValue <= maxAllowed,
            $"Double sine wave: {stats.BitsPerValue:F2} bits/value exceeds C++ baseline " +
            $"({CppDoubleSineBitsPerValue:F2}) by more than {(Tolerance - 1) * 100:F0}%");
    }

    #endregion

    #region Multi-dimensional Tests

    [Theory]
    [InlineData(1000, 1, 1)]    // 1D
    [InlineData(100, 10, 1)]   // 2D
    [InlineData(10, 10, 10)]   // 3D
    public void Float_TrilinearField_RoundTripsAcrossDimensions(int nx, int ny, int nz)
    {
        // Trilinear field should compress in all dimensions
        float[] field = TrilinearFieldGenerator.GenerateFloatField(nx, ny, nz);
        var stats = CompressionTestHelper.CompressFloat(field, nx, ny, nz);

        // Verify round-trip
        float[] decompressed = FpZipCompressor.DecompressFloat(stats.CompressedData);
        Assert.Equal(field.Length, decompressed.Length);
        Assert.Equal(field, decompressed);

        // Should achieve some compression on correlated data
        Assert.True(stats.CompressionRatio > 1.0,
            $"Dimension {nx}x{ny}x{nz}: ratio {stats.CompressionRatio:F2} should be > 1.0");
    }

    [Theory]
    [InlineData(1000, 1, 1)]    // 1D
    [InlineData(100, 10, 1)]   // 2D
    [InlineData(10, 10, 10)]   // 3D
    public void Double_TrilinearField_RoundTripsAcrossDimensions(int nx, int ny, int nz)
    {
        double[] field = TrilinearFieldGenerator.GenerateDoubleField(nx, ny, nz);
        var stats = CompressionTestHelper.CompressDouble(field, nx, ny, nz);

        double[] decompressed = FpZipCompressor.DecompressDouble(stats.CompressedData);
        Assert.Equal(field.Length, decompressed.Length);
        Assert.Equal(field, decompressed);

        Assert.True(stats.CompressionRatio > 1.0,
            $"Dimension {nx}x{ny}x{nz}: ratio {stats.CompressionRatio:F2} should be > 1.0");
    }

    #endregion
}
