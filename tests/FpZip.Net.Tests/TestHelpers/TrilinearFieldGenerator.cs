namespace FpZip.Tests.TestHelpers;

/// <summary>
/// Generates trilinear fields for compression testing.
/// Ported from the C++ reference implementation (testfpzip.c).
/// </summary>
public static class TrilinearFieldGenerator
{
    /// <summary>
    /// LCG multiplier from C++ reference.
    /// </summary>
    private const uint Multiplier = 1103515245;

    /// <summary>
    /// LCG increment from C++ reference.
    /// </summary>
    private const uint Increment = 12345;

    /// <summary>
    /// Generates the next random double using the C++ LCG algorithm.
    /// Range: [-1, 1], shaped by cubing twice (val^9).
    /// </summary>
    private static double NextDouble(ref uint seed)
    {
        // Linear Congruential Generator step
        seed = Multiplier * seed + Increment;
        seed &= 0x7FFFFFFFu;  // Keep only 31 bits

        // Convert to [0, 1)
        double val = Math.ScaleB((double)seed, -31);

        // Convert to [-1, 1]
        val = 2 * val - 1;

        // Shape distribution by cubing twice (val^9)
        val *= val * val;  // val^3
        val *= val * val;  // val^9

        return val;
    }

    /// <summary>
    /// Generates the next random float using the C++ LCG algorithm.
    /// </summary>
    private static float NextFloat(ref uint seed)
    {
        return (float)NextDouble(ref seed);
    }

    /// <summary>
    /// Generates a trilinear float field perturbed by random noise.
    /// Matches the C++ float_field() function exactly.
    /// </summary>
    /// <param name="nx">X dimension (default: 65)</param>
    /// <param name="ny">Y dimension (default: 64)</param>
    /// <param name="nz">Z dimension (default: 63)</param>
    /// <param name="offset">Offset value for first element (default: 0)</param>
    /// <param name="seed">Initial PRNG seed (default: 1)</param>
    /// <returns>Float array of size nx*ny*nz with trilinear field data</returns>
    public static float[] GenerateFloatField(
        int nx = 65, int ny = 64, int nz = 63,
        float offset = 0f, uint seed = 1)
    {
        int n = nx * ny * nz;
        float[] field = new float[n];

        // Generate random field: first element = offset, rest = random
        field[0] = offset;
        for (int i = 1; i < n; i++)
        {
            field[i] = NextFloat(ref seed);
        }

        // Integrate along X axis
        for (int z = 0; z < nz; z++)
        {
            for (int y = 0; y < ny; y++)
            {
                for (int x = 1; x < nx; x++)
                {
                    int idx = x + nx * (y + ny * z);
                    int prevIdx = (x - 1) + nx * (y + ny * z);
                    field[idx] += field[prevIdx];
                }
            }
        }

        // Integrate along Y axis
        for (int z = 0; z < nz; z++)
        {
            for (int y = 1; y < ny; y++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int idx = x + nx * (y + ny * z);
                    int prevIdx = x + nx * ((y - 1) + ny * z);
                    field[idx] += field[prevIdx];
                }
            }
        }

        // Integrate along Z axis
        for (int z = 1; z < nz; z++)
        {
            for (int y = 0; y < ny; y++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int idx = x + nx * (y + ny * z);
                    int prevIdx = x + nx * (y + ny * (z - 1));
                    field[idx] += field[prevIdx];
                }
            }
        }

        return field;
    }

    /// <summary>
    /// Generates a trilinear double field perturbed by random noise.
    /// Matches the C++ double_field() function exactly.
    /// </summary>
    /// <param name="nx">X dimension (default: 65)</param>
    /// <param name="ny">Y dimension (default: 64)</param>
    /// <param name="nz">Z dimension (default: 63)</param>
    /// <param name="offset">Offset value for first element (default: 0)</param>
    /// <param name="seed">Initial PRNG seed (default: 1)</param>
    /// <returns>Double array of size nx*ny*nz with trilinear field data</returns>
    public static double[] GenerateDoubleField(
        int nx = 65, int ny = 64, int nz = 63,
        double offset = 0.0, uint seed = 1)
    {
        int n = nx * ny * nz;
        double[] field = new double[n];

        // Generate random field: first element = offset, rest = random
        field[0] = offset;
        for (int i = 1; i < n; i++)
        {
            field[i] = NextDouble(ref seed);
        }

        // Integrate along X axis
        for (int z = 0; z < nz; z++)
        {
            for (int y = 0; y < ny; y++)
            {
                for (int x = 1; x < nx; x++)
                {
                    int idx = x + nx * (y + ny * z);
                    int prevIdx = (x - 1) + nx * (y + ny * z);
                    field[idx] += field[prevIdx];
                }
            }
        }

        // Integrate along Y axis
        for (int z = 0; z < nz; z++)
        {
            for (int y = 1; y < ny; y++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int idx = x + nx * (y + ny * z);
                    int prevIdx = x + nx * ((y - 1) + ny * z);
                    field[idx] += field[prevIdx];
                }
            }
        }

        // Integrate along Z axis
        for (int z = 1; z < nz; z++)
        {
            for (int y = 0; y < ny; y++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int idx = x + nx * (y + ny * z);
                    int prevIdx = x + nx * (y + ny * (z - 1));
                    field[idx] += field[prevIdx];
                }
            }
        }

        return field;
    }
}
