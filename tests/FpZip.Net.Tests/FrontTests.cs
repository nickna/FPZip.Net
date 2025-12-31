using Xunit;
using FpZip.Core;

namespace FpZip.Tests;

public class FrontTests
{
    [Fact]
    public void Front_InitializesToZero()
    {
        var front = new Front<uint>(3, 3, 0u);

        // All values should be zero initially
        Assert.Equal(0u, front[0, 0, 0]);
        Assert.Equal(0u, front[1, 0, 0]);
        Assert.Equal(0u, front[0, 1, 0]);
        Assert.Equal(0u, front[0, 0, 1]);
    }

    [Fact]
    public void Front_Push_StoresValue()
    {
        var front = new Front<uint>(3, 3, 0u);

        front.Push(42u);

        // After push, [1,0,0] accesses the most recently pushed value
        // (index is incremented after push, so [1,0,0] looks back 1 position)
        Assert.Equal(42u, front[1, 0, 0]);
    }

    [Fact]
    public void Front_Push_Multiple_StoresValues()
    {
        var front = new Front<uint>(3, 3, 0u);

        front.Push(1u);
        front.Push(2u);
        front.Push(3u);

        // After 3 pushes, [1,0,0] is the most recent, [2,0,0] is second most recent
        Assert.Equal(3u, front[1, 0, 0]);
        Assert.Equal(2u, front[2, 0, 0]);
        Assert.Equal(1u, front[3, 0, 0]);
    }

    [Fact]
    public void Front_Advance_FillsWithZeros()
    {
        var front = new Front<uint>(3, 3, 0u);

        front.Push(100u);
        front.Advance(1, 0, 0);

        // After advance(1,0,0), one zero was pushed
        // So [1,0,0] = 0 (the zero from advance), [2,0,0] = 100 (our original value)
        Assert.Equal(0u, front[1, 0, 0]);
        Assert.Equal(100u, front[2, 0, 0]);
    }

    [Fact]
    public void Front_CircularBehavior_Works()
    {
        var front = new Front<uint>(2, 2, 0u);

        // Push enough values to wrap around
        for (uint i = 0; i < 100; i++)
        {
            front.Push(i);
        }

        // Should still work without errors - [1,0,0] gets most recent value
        Assert.Equal(99u, front[1, 0, 0]);
    }
}
