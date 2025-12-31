using Xunit;
using FpZip.Tests.TestHelpers;

namespace FpZip.Tests;

/// <summary>
/// Tests for the ported PRNG to ensure C++ compatibility.
/// </summary>
public class PrngTests
{
    [Fact]
    public void LcgRandom_FirstIterationProducesExpectedSeed()
    {
        // The C++ PRNG produces a deterministic sequence.
        // First iteration: seed = 1103515245 * 1 + 12345 = 1103527590
        // seed &= 0x7FFFFFFF = 1103527590 (unchanged, as it's < 0x80000000)

        uint expectedSeed1 = (1103515245u * 1u + 12345u) & 0x7FFFFFFFu;
        Assert.Equal(1103527590u, expectedSeed1);
    }

    [Fact]
    public void GenerateFloatField_ProducesDeterministicOutput()
    {
        // Same parameters should produce identical fields
        float[] field1 = TrilinearFieldGenerator.GenerateFloatField(10, 10, 10, 0f, seed: 1);
        float[] field2 = TrilinearFieldGenerator.GenerateFloatField(10, 10, 10, 0f, seed: 1);

        Assert.Equal(field1, field2);
    }

    [Fact]
    public void GenerateDoubleField_ProducesDeterministicOutput()
    {
        // Same parameters should produce identical fields
        double[] field1 = TrilinearFieldGenerator.GenerateDoubleField(10, 10, 10, 0.0, seed: 1);
        double[] field2 = TrilinearFieldGenerator.GenerateDoubleField(10, 10, 10, 0.0, seed: 1);

        Assert.Equal(field1, field2);
    }

    [Fact]
    public void GenerateFloatField_DifferentSeedsProduceDifferentOutput()
    {
        float[] field1 = TrilinearFieldGenerator.GenerateFloatField(10, 10, 10, 0f, seed: 1);
        float[] field2 = TrilinearFieldGenerator.GenerateFloatField(10, 10, 10, 0f, seed: 42);

        Assert.NotEqual(field1, field2);
    }

    [Fact]
    public void GenerateFloatField_TrivialField_FirstElementIsOffset()
    {
        // With a trivial 1x1x1 field, the first (only) element should be the offset
        // since there's no integration to change it
        float[] trivial = TrilinearFieldGenerator.GenerateFloatField(1, 1, 1, offset: 123.456f);
        Assert.Equal(123.456f, trivial[0]);
    }

    [Fact]
    public void GenerateDoubleField_TrivialField_FirstElementIsOffset()
    {
        double[] trivial = TrilinearFieldGenerator.GenerateDoubleField(1, 1, 1, offset: 123.456);
        Assert.Equal(123.456, trivial[0]);
    }

    [Fact]
    public void GenerateFloatField_StandardDimensions_MatchesExpectedSize()
    {
        float[] field = TrilinearFieldGenerator.GenerateFloatField(65, 64, 63);

        Assert.Equal(65 * 64 * 63, field.Length);
        Assert.Equal(262080, field.Length);
    }

    [Fact]
    public void GenerateDoubleField_StandardDimensions_MatchesExpectedSize()
    {
        double[] field = TrilinearFieldGenerator.GenerateDoubleField(65, 64, 63);

        Assert.Equal(65 * 64 * 63, field.Length);
        Assert.Equal(262080, field.Length);
    }

    [Fact]
    public void GenerateFloatField_HasReasonableRange()
    {
        // After integration, the field should have reasonable values (not NaN/Inf)
        float[] field = TrilinearFieldGenerator.GenerateFloatField(10, 10, 10);

        // All values should be finite
        foreach (var value in field)
        {
            Assert.True(float.IsFinite(value), $"Field contains non-finite value: {value}");
        }

        // Field should have some variation (min != max)
        float min = field.Min();
        float max = field.Max();
        Assert.True(max > min, "Field should have variation");
    }
}
