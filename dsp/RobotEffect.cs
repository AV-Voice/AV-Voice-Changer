namespace VybeShift.Core.DSP;

/// <summary>
/// Robot voice via STFT phase-resetting.
/// Forces all spectral phase components to zero each frame → flat, mechanical timbre.
/// Uses overlapping windows for smooth resynthesis.
/// </summary>
public sealed class RobotEffect : IAudioEffect
{
    public string Name      { get; } = "Robot";
    public bool   IsEnabled { get; set; }

    private float _phase = 0f;
    private const float CARRIER_FREQ = 60f; // 60Hz robotic hum
    private const float SAMPLE_RATE  = 48000f;

    public void Process(Span<float> buf)
    {
        float phaseInc = MathF.Tau * CARRIER_FREQ / SAMPLE_RATE;

        for (int i = 0; i < buf.Length; i++)
        {
            // Simple ring modulation with a sine wave creates a classic Dalek/Robot timbre
            // very efficiently without FFT latency or phase cancellation dropouts.
            buf[i] *= MathF.Sin(_phase);

            _phase += phaseInc;
            if (_phase > MathF.Tau) _phase -= MathF.Tau;
        }
    }

    public void Reset()
    {
        _phase = 0f;
    }
}
