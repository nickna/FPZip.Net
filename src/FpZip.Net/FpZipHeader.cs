using System.Buffers.Binary;

namespace FpZip;

/// <summary>
/// Type of floating-point data.
/// </summary>
public enum FpZipType
{
    Float = 0,
    Double = 1
}

/// <summary>
/// Header structure for FpZip compressed data.
/// </summary>
public readonly struct FpZipHeader
{
    /// <summary>
    /// Magic number identifying FpZip format: 'fpz\0'
    /// </summary>
    public const uint MagicNumber = 0x007A7066; // 'fpz\0' in little-endian

    /// <summary>
    /// Current format version.
    /// </summary>
    public const ushort Version = 1;

    /// <summary>
    /// Header size in bytes.
    /// </summary>
    public const int HeaderSize = 24; // 4 (magic) + 2 (version) + 1 (type) + 1 (reserved) + 4*4 (dimensions)

    /// <summary>
    /// Type of data (float or double).
    /// </summary>
    public FpZipType Type { get; }

    /// <summary>
    /// X dimension size.
    /// </summary>
    public int Nx { get; }

    /// <summary>
    /// Y dimension size.
    /// </summary>
    public int Ny { get; }

    /// <summary>
    /// Z dimension size.
    /// </summary>
    public int Nz { get; }

    /// <summary>
    /// Number of fields.
    /// </summary>
    public int Nf { get; }

    /// <summary>
    /// Total number of elements (Nx * Ny * Nz * Nf).
    /// </summary>
    public long TotalElements => (long)Nx * Ny * Nz * Nf;

    /// <summary>
    /// Creates a new FpZip header.
    /// </summary>
    public FpZipHeader(FpZipType type, int nx, int ny = 1, int nz = 1, int nf = 1)
    {
        if (nx <= 0) throw new ArgumentOutOfRangeException(nameof(nx), "Must be positive");
        if (ny <= 0) throw new ArgumentOutOfRangeException(nameof(ny), "Must be positive");
        if (nz <= 0) throw new ArgumentOutOfRangeException(nameof(nz), "Must be positive");
        if (nf <= 0) throw new ArgumentOutOfRangeException(nameof(nf), "Must be positive");

        Type = type;
        Nx = nx;
        Ny = ny;
        Nz = nz;
        Nf = nf;
    }

    /// <summary>
    /// Writes the header to a stream.
    /// </summary>
    public void WriteTo(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[HeaderSize];

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[0..], MagicNumber);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], Version);
        buffer[6] = (byte)Type;
        buffer[7] = 0; // reserved
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], Nx);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], Ny);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], Nz);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[20..], Nf);

        stream.Write(buffer);
    }

    /// <summary>
    /// Reads a header from a stream.
    /// </summary>
    public static FpZipHeader ReadFrom(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[HeaderSize];
        int bytesRead = stream.ReadAtLeast(buffer, HeaderSize, throwOnEndOfStream: true);

        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(buffer[0..]);
        if (magic != MagicNumber)
            throw new InvalidDataException($"Invalid magic number. Expected 0x{MagicNumber:X8}, got 0x{magic:X8}");

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(buffer[4..]);
        if (version > Version)
            throw new InvalidDataException($"Unsupported version {version}. Maximum supported: {Version}");

        var type = (FpZipType)buffer[6];
        if (type != FpZipType.Float && type != FpZipType.Double)
            throw new InvalidDataException($"Invalid type: {(int)type}");

        int nx = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]);
        int ny = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]);
        int nz = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]);
        int nf = BinaryPrimitives.ReadInt32LittleEndian(buffer[20..]);

        return new FpZipHeader(type, nx, ny, nz, nf);
    }

    /// <summary>
    /// Reads a header from a byte span.
    /// </summary>
    public static FpZipHeader ReadFrom(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize)
            throw new ArgumentException($"Data too short. Need at least {HeaderSize} bytes.", nameof(data));

        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data[0..]);
        if (magic != MagicNumber)
            throw new InvalidDataException($"Invalid magic number. Expected 0x{MagicNumber:X8}, got 0x{magic:X8}");

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        if (version > Version)
            throw new InvalidDataException($"Unsupported version {version}. Maximum supported: {Version}");

        var type = (FpZipType)data[6];
        if (type != FpZipType.Float && type != FpZipType.Double)
            throw new InvalidDataException($"Invalid type: {(int)type}");

        int nx = BinaryPrimitives.ReadInt32LittleEndian(data[8..]);
        int ny = BinaryPrimitives.ReadInt32LittleEndian(data[12..]);
        int nz = BinaryPrimitives.ReadInt32LittleEndian(data[16..]);
        int nf = BinaryPrimitives.ReadInt32LittleEndian(data[20..]);

        return new FpZipHeader(type, nx, ny, nz, nf);
    }
}
