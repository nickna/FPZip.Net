using Xunit;
using FpZip.Core;

namespace FpZip.Tests;

public class PCMapTests
{
    [Fact]
    public void Float_ForwardInverse_RoundTrip()
    {
        float[] testValues = { 0f, 1f, -1f, 0.5f, -0.5f, float.MaxValue, float.MinValue, float.Epsilon };

        foreach (float original in testValues)
        {
            uint mapped = PCMap.Forward(original);
            float recovered = PCMap.Inverse(mapped);
            Assert.Equal(original, recovered);
        }
    }

    [Fact]
    public void Double_ForwardInverse_RoundTrip()
    {
        double[] testValues = { 0d, 1d, -1d, 0.5d, -0.5d, double.MaxValue, double.MinValue, double.Epsilon };

        foreach (double original in testValues)
        {
            ulong mapped = PCMap.Forward(original);
            double recovered = PCMap.Inverse(mapped);
            Assert.Equal(original, recovered);
        }
    }

    [Fact]
    public void Float_Forward_PreservesOrdering()
    {
        // Positive values: larger float should map to larger uint
        Assert.True(PCMap.Forward(1.0f) > PCMap.Forward(0.5f));
        Assert.True(PCMap.Forward(2.0f) > PCMap.Forward(1.0f));
        Assert.True(PCMap.Forward(100f) > PCMap.Forward(10f));
    }

    [Fact]
    public void Float_SpecialValues_RoundTrip()
    {
        // Test special IEEE 754 values
        float[] specialValues = { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -0f };

        foreach (float original in specialValues)
        {
            uint mapped = PCMap.Forward(original);
            float recovered = PCMap.Inverse(mapped);

            if (float.IsNaN(original))
                Assert.True(float.IsNaN(recovered));
            else
                Assert.Equal(original, recovered);
        }
    }

    [Fact]
    public void Double_SpecialValues_RoundTrip()
    {
        // Test special IEEE 754 values
        double[] specialValues = { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0d };

        foreach (double original in specialValues)
        {
            ulong mapped = PCMap.Forward(original);
            double recovered = PCMap.Inverse(mapped);

            if (double.IsNaN(original))
                Assert.True(double.IsNaN(recovered));
            else
                Assert.Equal(original, recovered);
        }
    }
}
