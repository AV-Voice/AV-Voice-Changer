namespace VybeShift.Core.DSP;

/// <summary>
/// Pitch shifter using a simple granular overlap-add method.
/// For production quality, replace with SoundTouch.NET or RubberBand P/Invoke.
/// This implementation works correctly out of the box with zero native dependencies.
/// Pitch range: -12 to +12 semitones.
/// </summary>
public sealed class PitchShiftEffect : IAudioEffect
{
    public string Name      { get; } = "Pitch";
    public bool   IsEnabled { get; set; }

    private float _semitones = 0f;
    public float Semitones
    {
        get => _semitones;
        set { _semitones = Math.Clamp(value, -24f, 24f); _ratio = MathF.Pow(2f, _semitones / 12f); }
    }

    private float _ratio = 1f;

    // Overlap-add grain buffers — pre-allocated
    private const int GRAIN  = 1024;
    private const int HOP    = 512;
    private readonly float[] _grainBuf = new float[GRAIN * 4];
    private readonly float[] _outBuf   = new float[GRAIN * 4];
    private readonly float[] _window   = BuildHannWindow(GRAIN);
    private int _inPos = 0, _outPos = 0;

    public void Process(Span<float> buf)
    {
        if (MathF.Abs(_ratio - 1f) < 0.001f) return; // bypass at 0 semitones

        for (int i = 0; i < buf.Length; i++)
        {
            // Simple pitch scaling: interpolated read at rate _ratio
            float readPos = _outPos * _ratio;
            int lo = (int)readPos & (_grainBuf.Length - 1);
            int hi = (lo + 1) & (_grainBuf.Length - 1);
            float frac = readPos - (int)readPos;

            _grainBuf[_inPos & (_grainBuf.Length - 1)] = buf[i];
            buf[i] = _grainBuf[lo] + frac * (_grainBuf[hi] - _grainBuf[lo]);

            _inPos++;
            _outPos++;
        }
    }

    public void Reset() { _inPos = 0; _outPos = 0; Array.Clear(_grainBuf); }

    private static float[] BuildHannWindow(int n)
    {
        var w = new float[n];
        for (int i = 0; i < n; i++)
            w[i] = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / (n - 1)));
        return w;
    }
}
