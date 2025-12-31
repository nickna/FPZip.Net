using Xunit;
using Xunit.Abstractions;
using FpZip.Tests.TestHelpers;

namespace FpZip.Tests;

/// <summary>
/// Benchmark tests that output compression ratios for comparison with C++.
/// </summary>
public class CompressionBenchmark
{
    private readonly ITestOutputHelper _output;

    public CompressionBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BenchmarkAllPatterns()
    {
        _output.WriteLine("=== FpZip.Net Compression Ratio Benchmark ===\n");

        // Standard test dimensions from C++ (65x64x63)
        const int nx = 65, ny = 64, nz = 63;
        int totalElements = nx * ny * nz;

        _output.WriteLine($"Dimensions: {nx} x {ny} x {nz} = {totalElements:N0} elements\n");

        // Test 1: Trilinear field (standard C++ test data)
        _output.WriteLine("--- Trilinear Field (C++ reference test data) ---");
        var floatField = TrilinearFieldGenerator.GenerateFloatField(nx, ny, nz);
        var floatStats = CompressionTestHelper.CompressFloat(floatField, nx, ny, nz);
        _output.WriteLine($"Float:  {floatStats.BitsPerValue:F2} bits/value, ratio {floatStats.CompressionRatio:F2}:1");

        var doubleField = TrilinearFieldGenerator.GenerateDoubleField(nx, ny, nz);
        var doubleStats = CompressionTestHelper.CompressDouble(doubleField, nx, ny, nz);
        _output.WriteLine($"Double: {doubleStats.BitsPerValue:F2} bits/value, ratio {doubleStats.CompressionRatio:F2}:1\n");

        // Test 2: Constant data
        _output.WriteLine("--- Constant Data (all 3.14159) ---");
        var constFloat = new float[totalElements];
        Array.Fill(constFloat, 3.14159f);
        var constFloatStats = CompressionTestHelper.CompressFloat(constFloat, nx, ny, nz);
        _output.WriteLine($"Float:  {constFloatStats.BitsPerValue:F2} bits/value, ratio {constFloatStats.CompressionRatio:F2}:1");

        var constDouble = new double[totalElements];
        Array.Fill(constDouble, 3.14159265358979);
        var constDoubleStats = CompressionTestHelper.CompressDouble(constDouble, nx, ny, nz);
        _output.WriteLine($"Double: {constDoubleStats.BitsPerValue:F2} bits/value, ratio {constDoubleStats.CompressionRatio:F2}:1\n");

        // Test 3: All zeros
        _output.WriteLine("--- All Zeros ---");
        var zerosFloat = new float[totalElements];
        var zerosFloatStats = CompressionTestHelper.CompressFloat(zerosFloat, nx, ny, nz);
        _output.WriteLine($"Float:  {zerosFloatStats.BitsPerValue:F2} bits/value, ratio {zerosFloatStats.CompressionRatio:F2}:1");

        var zerosDouble = new double[totalElements];
        var zerosDoubleStats = CompressionTestHelper.CompressDouble(zerosDouble, nx, ny, nz);
        _output.WriteLine($"Double: {zerosDoubleStats.BitsPerValue:F2} bits/value, ratio {zerosDoubleStats.CompressionRatio:F2}:1\n");

        // Test 4: Linear gradient
        _output.WriteLine("--- Linear Gradient ---");
        var gradFloat = new float[totalElements];
        for (int i = 0; i < totalElements; i++) gradFloat[i] = i * 0.001f;
        var gradFloatStats = CompressionTestHelper.CompressFloat(gradFloat, nx, ny, nz);
        _output.WriteLine($"Float:  {gradFloatStats.BitsPerValue:F2} bits/value, ratio {gradFloatStats.CompressionRatio:F2}:1");

        var gradDouble = new double[totalElements];
        for (int i = 0; i < totalElements; i++) gradDouble[i] = i * 0.001;
        var gradDoubleStats = CompressionTestHelper.CompressDouble(gradDouble, nx, ny, nz);
        _output.WriteLine($"Double: {gradDoubleStats.BitsPerValue:F2} bits/value, ratio {gradDoubleStats.CompressionRatio:F2}:1\n");

        // Test 5: Sine wave
        _output.WriteLine("--- Sine Wave ---");
        var sineFloat = new float[totalElements];
        for (int i = 0; i < totalElements; i++) sineFloat[i] = MathF.Sin(i * 0.01f) * 100f;
        var sineFloatStats = CompressionTestHelper.CompressFloat(sineFloat, nx, ny, nz);
        _output.WriteLine($"Float:  {sineFloatStats.BitsPerValue:F2} bits/value, ratio {sineFloatStats.CompressionRatio:F2}:1");

        var sineDouble = new double[totalElements];
        for (int i = 0; i < totalElements; i++) sineDouble[i] = Math.Sin(i * 0.01) * 100.0;
        var sineDoubleStats = CompressionTestHelper.CompressDouble(sineDouble, nx, ny, nz);
        _output.WriteLine($"Double: {sineDoubleStats.BitsPerValue:F2} bits/value, ratio {sineDoubleStats.CompressionRatio:F2}:1\n");

        // Test 6: Random data
        _output.WriteLine("--- Random Data ---");
        var rng = new Random(42);
        var randFloat = new float[totalElements];
        for (int i = 0; i < totalElements; i++) randFloat[i] = (float)(rng.NextDouble() * 1000 - 500);
        var randFloatStats = CompressionTestHelper.CompressFloat(randFloat, nx, ny, nz);
        _output.WriteLine($"Float:  {randFloatStats.BitsPerValue:F2} bits/value, ratio {randFloatStats.CompressionRatio:F2}:1");

        rng = new Random(42);
        var randDouble = new double[totalElements];
        for (int i = 0; i < totalElements; i++) randDouble[i] = rng.NextDouble() * 1000 - 500;
        var randDoubleStats = CompressionTestHelper.CompressDouble(randDouble, nx, ny, nz);
        _output.WriteLine($"Double: {randDoubleStats.BitsPerValue:F2} bits/value, ratio {randDoubleStats.CompressionRatio:F2}:1\n");

        _output.WriteLine("=== End Benchmark ===");
    }
}
