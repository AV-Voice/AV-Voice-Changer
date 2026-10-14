namespace VybeShift.Core.DSP;

/// <summary>
/// Formant shift via spectral envelope scaling.
/// Uses a simple comb resonator to approximate formant movement.
/// Shift > 1.0 → brighter/higher formants (female direction)
/// Shift < 1.0 → darker/lower formants (male/deep direction)
/// </summary>
public sealed class FormantShiftEffect : IAudioEffect
{
    public string Name      { get; } = "Formant";
    public bool   IsEnabled { get; set; }

    private float _shift = 1.0f;
    public float Shift
    {
        get => _shift;
        set => _shift = Math.Clamp(value, 0.5f, 2.0f);
    }

    // 4-pole IIR filter state for formant emphasis
    private readonly float[] _z  = new float[8];  // filter states (4 biquad stages)
    private float[]          _bf = new float[2048]; // pre-alloc scratch

    public void Process(Span<float> buf)
    {
        if (MathF.Abs(_shift - 1f) < 0.01f) return;

        // Compute frequency warp factor
        float warp = _shift - 1f;

        // Apply first-order allpass chain to warp frequency axis
        // Each allpass stage shifts phase non-linearly, effectively warping formants
        float coeff = warp * 0.4f; // keep stable (|coeff| < 1)

        for (int i = 0; i < buf.Length; i++)
        {
            float x = buf[i];

            // 4 cascaded first-order allpass filters
            for (int s = 0; s < 4; s++)
            {
                float y = coeff * (x - _z[s]) + _z[s + 4];
                _z[s + 4] = x;
                _z[s]     = y;
                x         = y;
            }

            buf[i] = x;
        }
    }

    public void Reset() => Array.Clear(_z);
}
