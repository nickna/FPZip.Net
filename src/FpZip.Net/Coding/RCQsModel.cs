using System.Runtime.CompilerServices;

namespace FpZip.Coding;

/// <summary>
/// Quasi-Static adaptive probability model for the range coder.
/// Maintains symbol frequencies that adapt during encoding/decoding.
/// </summary>
public sealed class RCQsModel
{
    private const int TableShift = 7;

    public int Symbols { get; }

    private readonly int _bits;           // number of bits of precision for frequencies
    private int _left;                    // number of symbols until next normalization
    private int _more;                    // number of symbols with larger increment
    private uint _incr;                   // increment per update
    private int _rescale;                 // current interval between normalizations
    private readonly int _targetRescale;  // target interval between rescales
    private readonly uint[] _symf;        // array of partially updated frequencies
    private readonly uint[] _cumf;        // array of cumulative frequencies
    private readonly int _searchShift;    // difference of frequency bits and table bits
    private readonly uint[]? _search;     // structure for searching on decompression

    /// <summary>
    /// Creates a new quasi-static probability model.
    /// </summary>
    /// <param name="compress">true for compression, false for decompression</param>
    /// <param name="symbols">number of symbols</param>
    /// <param name="bits">log2 of total frequency count (must be &lt;= 16)</param>
    /// <param name="period">max symbols between normalizations</param>
    public RCQsModel(bool compress, int symbols, int bits = 16, int period = 0x400)
    {
        if (bits > 16)
            throw new ArgumentException("bits must be <= 16", nameof(bits));
        if (period >= (1 << (bits + 1)))
            throw new ArgumentException("period too large", nameof(period));

        Symbols = symbols;
        _bits = bits;
        _targetRescale = period;

        int n = symbols;
        _symf = new uint[n + 1];
        _cumf = new uint[n + 1];
        _cumf[0] = 0;
        _cumf[n] = 1u << bits;

        if (compress)
        {
            _search = null;
            _searchShift = 0;
        }
        else
        {
            _searchShift = bits - TableShift;
            _search = new uint[(1 << TableShift) + 1];
        }

        Reset();
    }

    /// <summary>
    /// Reinitializes the model to uniform distribution.
    /// </summary>
    public void Reset()
    {
        int n = Symbols;
        _rescale = (n >> 4) | 2;
        _more = 0;

        uint totalFreq = _cumf[n];
        uint f = totalFreq / (uint)n;
        uint m = totalFreq % (uint)n;

        for (int i = 0; i < (int)m; i++)
            _symf[i] = f + 1;
        for (int i = (int)m; i < n; i++)
            _symf[i] = f;

        Update();
    }

    /// <summary>
    /// Gets the cumulative and individual frequencies for encoding symbol s.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(uint s, out uint cumFreq, out uint freq)
    {
        cumFreq = _cumf[s];
        freq = _cumf[s + 1] - cumFreq;
        UpdateSymbol(s);
    }

    /// <summary>
    /// Returns the symbol corresponding to cumulative frequency l.
    /// </summary>
    public uint Decode(ref uint l, out uint r)
    {
        int i = (int)(l >> _searchShift);
        uint s = _search![i];
        uint h = _search[i + 1] + 1;

        // Find symbol via binary search
        while (s + 1 < h)
        {
            uint m = (s + h) >> 1; // Bit shift instead of division
            if (l < _cumf[m])
                h = m;
            else
                s = m;
        }

        l = _cumf[s];
        r = _cumf[s + 1] - l;
        UpdateSymbol(s);

        return s;
    }

    /// <summary>
    /// Normalizes the range by shifting right by bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize(ref uint r)
    {
        r >>= _bits;
    }

    /// <summary>
    /// Main update routine - rescales frequencies and rebuilds tables.
    /// </summary>
    private void Update()
    {
        if (_more > 0)
        {
            // We have some more symbols before update
            _left = _more;
            _more = 0;
            _incr++;
            return;
        }

        if (_rescale != _targetRescale)
        {
            _rescale *= 2;
            if (_rescale > _targetRescale)
                _rescale = _targetRescale;
        }

        // Update symbol frequencies
        int n = Symbols;
        uint cf = _cumf[n];
        uint count = cf;

        for (int i = n - 1; i >= 0; i--)
        {
            uint sf = _symf[i];
            cf -= sf;
            _cumf[i] = cf;
            sf = (sf >> 1) | 1; // halve with odd bit set
            count -= sf;
            _symf[i] = sf;
        }

        // count is now difference between target cumf[n] and sum of symf;
        // next actual rescale happens when sum of symf equals cumf[n]
        _incr = count / (uint)_rescale;
        _more = (int)(count % (uint)_rescale);
        _left = _rescale - _more;

        // Build lookup table for fast symbol searches
        if (_search != null)
        {
            int h = 1 << TableShift;
            for (int i = n - 1; i >= 0; i--)
            {
                int newH = (int)(_cumf[i] >> _searchShift);
                for (int l = newH; l <= h; l++)
                    _search[l] = (uint)i;
                h = newH;
            }
        }
    }

    /// <summary>
    /// Updates frequency for a single symbol.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSymbol(uint s)
    {
        if (_left == 0)
            Update();
        _left--;
        _symf[s] += _incr;
    }
}
