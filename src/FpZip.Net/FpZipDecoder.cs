using FpZip.Coding;
using FpZip.Core;

namespace FpZip;

/// <summary>
/// Internal decoder for FpZip decompression.
/// </summary>
internal static class FpZipDecoder
{
    /// <summary>
    /// Decompresses a 3D float array.
    /// </summary>
    public static void Decompress3DFloat(
        RangeDecoder decoder,
        Span<float> data,
        int nx, int ny, int nz)
    {
        // Initialize decompressor with integer arithmetic mode
        var model = new RCQsModel(compress: false, PCDecoderFloat.Symbols);
        var pcDecoder = new PCDecoderFloat(decoder, model);

        // Initialize front with mapped zero value
        uint zeroMapped = PCMap.Forward(0f);
        var front = new Front<uint>(nx, ny, zeroMapped);

        int dataIndex = 0;

        // Decode difference between predicted (p) and actual (a) value
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

                    // Decode actual value
                    uint a = pcDecoder.Decode(p);

                    // Convert back to float and store
                    data[dataIndex++] = PCMap.Inverse(a);

                    // Push to front
                    front.Push(a);
                }
            }
        }
    }

    /// <summary>
    /// Decompresses a 3D double array.
    /// </summary>
    public static void Decompress3DDouble(
        RangeDecoder decoder,
        Span<double> data,
        int nx, int ny, int nz)
    {
        // Initialize decompressor with integer arithmetic mode
        var model = new RCQsModel(compress: false, PCDecoderDouble.Symbols);
        var pcDecoder = new PCDecoderDouble(decoder, model);

        // Initialize front with mapped zero value
        ulong zeroMapped = PCMap.Forward(0d);
        var front = new Front<ulong>(nx, ny, zeroMapped);

        int dataIndex = 0;

        // Decode difference between predicted (p) and actual (a) value
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

                    // Decode actual value
                    ulong a = pcDecoder.Decode(p);

                    // Convert back to double and store
                    data[dataIndex++] = PCMap.Inverse(a);

                    // Push to front
                    front.Push(a);
                }
            }
        }
    }

    /// <summary>
    /// Decompresses a 4D float array (multiple fields).
    /// </summary>
    public static void Decompress4DFloat(
        RangeDecoder decoder,
        Span<float> data,
        int nx, int ny, int nz, int nf)
    {
        int fieldSize = nx * ny * nz;
        for (int f = 0; f < nf; f++)
        {
            Decompress3DFloat(decoder, data.Slice(f * fieldSize, fieldSize), nx, ny, nz);
        }
    }

    /// <summary>
    /// Decompresses a 4D double array (multiple fields).
    /// </summary>
    public static void Decompress4DDouble(
        RangeDecoder decoder,
        Span<double> data,
        int nx, int ny, int nz, int nf)
    {
        int fieldSize = nx * ny * nz;
        for (int f = 0; f < nf; f++)
        {
            Decompress3DDouble(decoder, data.Slice(f * fieldSize, fieldSize), nx, ny, nz);
        }
    }
}
