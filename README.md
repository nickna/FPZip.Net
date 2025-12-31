# FpZip.Net

A pure managed C# implementation of the FPZip floating-point compression algorithm. This library provides lossless compression for multi-dimensional float and double arrays, optimized for scientific and numerical data.

## Features

- **Lossless compression** for `float` and `double` arrays
- **Multi-dimensional support** (1D, 2D, 3D, and 4D arrays)
- **Pure managed code** - no native dependencies
- **High performance** with `Span<T>` support
- **Simple API** - compress/decompress in one line

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package FpZip.Net
```

Or using Package Manager Console:
```
Install-Package FpZip.Net
```

## Quick Start

```csharp
using FpZip;

// Compress a float array
float[] data = { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f };
byte[] compressed = FpZipCompressor.Compress(data, nx: 8);

// Decompress back to float array
float[] decompressed = FpZipCompressor.DecompressFloat(compressed);
```

## API Reference

### Compression

```csharp
// 1D array
byte[] compressed = FpZipCompressor.Compress(floatArray, nx: length);

// 2D array (row-major order)
byte[] compressed = FpZipCompressor.Compress(floatArray, nx: width, ny: height);

// 3D array
byte[] compressed = FpZipCompressor.Compress(floatArray, nx: x, ny: y, nz: z);

// 4D array (multiple fields)
byte[] compressed = FpZipCompressor.Compress(floatArray, nx: x, ny: y, nz: z, nf: fields);

// Double arrays use the same API
byte[] compressed = FpZipCompressor.Compress(doubleArray, nx: length);

// Stream-based compression
FpZipCompressor.Compress(data.AsSpan(), outputStream, nx: x, ny: y, nz: z);
```

### Decompression

```csharp
// Decompress float data
float[] data = FpZipCompressor.DecompressFloat(compressedBytes);

// Decompress double data
double[] data = FpZipCompressor.DecompressDouble(compressedBytes);

// Read header without decompressing
FpZipHeader header = FpZipCompressor.ReadHeader(compressedBytes);
Console.WriteLine($"Dimensions: {header.Nx} x {header.Ny} x {header.Nz}");
Console.WriteLine($"Type: {header.Type}");
Console.WriteLine($"Total elements: {header.TotalElements}");
```

### Working with Spans

```csharp
// Compress from span
ReadOnlySpan<float> dataSpan = data.AsSpan();
byte[] compressed = FpZipCompressor.Compress(dataSpan, nx: 10, ny: 10, nz: 10);

// Decompress to pre-allocated buffer
Span<float> output = new float[1000];
FpZipHeader header = FpZipCompressor.Decompress(inputStream, output);
```

## Data Layout

Arrays are expected in row-major (C-style) order:

```csharp
// For a 3D array with dimensions nx=4, ny=3, nz=2:
// Index = x + nx * (y + ny * z)

float[] data = new float[4 * 3 * 2];
for (int z = 0; z < 2; z++)
    for (int y = 0; y < 3; y++)
        for (int x = 0; x < 4; x++)
            data[x + 4 * (y + 3 * z)] = GetValue(x, y, z);
```

## Algorithm

FpZip uses a combination of:

1. **Predictive coding** - A 3D wavefront predictor estimates each value from its neighbors
2. **Integer mapping** - IEEE 754 floats are mapped to integers preserving ordering
3. **Adaptive arithmetic coding** - A quasi-static probability model efficiently encodes prediction residuals

This approach achieves excellent compression ratios for smooth, continuous data typical in scientific computing.

## Requirements

- .NET 10.0 or later

## Building

```bash
cd FpZip.Net
dotnet build
```

## Running Tests

```bash
cd FpZip.Net
dotnet test
```

## File Format

The compressed format uses a simple header:

| Field | Size | Description |
|-------|------|-------------|
| Magic | 4 bytes | `fpz\0` (0x007A7066) |
| Version | 2 bytes | Format version |
| Type | 1 byte | 0 = float, 1 = double |
| Reserved | 1 byte | Reserved for future use |
| Nx | 4 bytes | X dimension |
| Ny | 4 bytes | Y dimension |
| Nz | 4 bytes | Z dimension |
| Nf | 4 bytes | Number of fields |
| Data | variable | Compressed data |

All multi-byte values are little-endian.

## License

This is a clean-room implementation based on the FPZip algorithm. The original FPZip library is available at https://github.com/LLNL/fpzip.

## Acknowledgments

- Original FPZip algorithm by Peter Lindstrom at Lawrence Livermore National Laboratory
- Based on the paper: P. Lindstrom and M. Isenburg, "Fast and Efficient Compression of Floating-Point Data," IEEE Transactions on Visualization and Computer Graphics, 2006.
