using FpZip.Coding;
using FpZip.Core;

namespace FpZip;

/// <summary>
/// Internal encoder for FpZip compression.
/// </summary>
internal static class FpZipEncoder
{
    /// <summary>
    /// Compresses a 3D float array.
    /// </summary>
    public static void Compress3DFloat(
        RangeEncoder encoder,
        ReadOnlySpan<float> data,
        int nx, int ny, int nz)
    {
        // Initialize compressor with integer arithmetic mode
        var model = new RCQsModel(compress: true, PCEncoderFloat.Symbols);
        var pcEncoder = new PCEncoderFloat(encoder, model);

        // Initialize front with mapped zero value
        uint zeroMapped = PCMap.Forward(0f);
        var front = new Front<uint>(nx, ny, zeroMapped);

        int dataIndex = 0;

        // Encode difference between predicted (p) and actual (a) value
        // C++ uses comma operator in for-loop initializers:
        //   for (z = 0, f.advance(0,0,1); z < nz; z++)  - advance runs ONCE before loop
        //     for (y = 0, f.advance(0,1,0); y < ny; y++)  - advance runs once per z
        //       for (x = 0, f.advance(1,0,0); x < nx; x++)  - advance runs once per y
        front.Advance(0, 0, 1);  // Once before entire loop
        for (int z = 0; z < nz; z++)
        {
            front.Advance(0, 1, 0);  // Once per z iteration
            for (int y = 0; y < ny; y++)
            {
                front.Advance(1, 0, 0);  // Once per y iteration
                for (int x = 0; x < nx; x++)
                {
                    // Calculate prediction from 7 neighbors (integer domain)
                    uint p = unchecked(
                        front[1, 0, 0] - front[0, 1, 1] +
                        front[0, 1, 0] - front[1, 0, 1] +
                        front[0, 0, 1] - front[1, 1, 0] +
                        front[1, 1, 1]);

                    // Map actual value to integer
                    uint a = PCMap.Forward(data[dataIndex++]);

                    // Encode and push to front
                    a = pcEncoder.Encode(a, p);
                    front.Push(a);
                }
            }
        }
    }

    /// <summary>
    /// Compresses a 3D double array.
    /// </summary>
    public static void Compress3DDouble(
        RangeEncoder encoder,
        ReadOnlySpan<double> data,
        int nx, int ny, int nz)
    {
        // Initialize compressor with integer arithmetic mode
        var model = new RCQsModel(compress: true, PCEncoderDouble.Symbols);
        var pcEncoder = new PCEncoderDouble(encoder, model);

        // Initialize front with mapped zero value
        ulong zeroMapped = PCMap.Forward(0d);
        var front = new Front<ulong>(nx, ny, zeroMapped);

        int dataIndex = 0;

        // Encode difference between predicted (p) and actual (a) value
        // C++ uses comma operator in for-loop initializers:
        //   for (z = 0, f.advance(0,0,1); z < nz; z++)  - advance runs ONCE before loop
        //     for (y = 0, f.advance(0,1,0); y < ny; y++)  - advance runs once per z
        //       for (x = 0, f.advance(1,0,0); x < nx; x++)  - advance runs once per y
        front.Advance(0, 0, 1);  // Once before entire loop
        for (int z = 0; z < nz; z++)
        {
            front.Advance(0, 1, 0);  // Once per z iteration
            for (int y = 0; y < ny; y++)
            {
                front.Advance(1, 0, 0);  // Once per y iteration
                for (int x = 0; x < nx; x++)
                {
                    // Calculate prediction from 7 neighbors (integer domain)
                    ulong p = unchecked(
                        front[1, 0, 0] - front[0, 1, 1] +
                        front[0, 1, 0] - front[1, 0, 1] +
                        front[0, 0, 1] - front[1, 1, 0] +
                        front[1, 1, 1]);

                    // Map actual value to integer
                    ulong a = PCMap.Forward(data[dataIndex++]);

                    // Encode and push to front
                    a = pcEncoder.Encode(a, p);
                    front.Push(a);
                }
            }
        }
    }

    /// <summary>
    /// Compresses a 4D float array (multiple fields).
    /// </summary>
    public static void Compress4DFloat(
        RangeEncoder encoder,
        ReadOnlySpan<float> data,
        int nx, int ny, int nz, int nf)
    {
        int fieldSize = nx * ny * nz;
        for (int f = 0; f < nf; f++)
        {
            Compress3DFloat(encoder, data.Slice(f * fieldSize, fieldSize), nx, ny, nz);
        }
    }

    /// <summary>
    /// Compresses a 4D double array (multiple fields).
    /// </summary>
    public static void Compress4DDouble(
        RangeEncoder encoder,
        ReadOnlySpan<double> data,
        int nx, int ny, int nz, int nf)
    {
        int fieldSize = nx * ny * nz;
        for (int f = 0; f < nf; f++)
        {
            Compress3DDouble(encoder, data.Slice(f * fieldSize, fieldSize), nx, ny, nz);
        }
    }
}
