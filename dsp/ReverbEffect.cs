namespace VybeShift.Core.DSP;

/// <summary>
/// Freeverb algorithm — 8 parallel comb filters + 4 allpass filters.
/// Near-zero CPU cost. Classic reverb sound.
/// </summary>
public sealed class ReverbEffect : IAudioEffect
{
    public string Name      { get; } = "Reverb";
    public bool   IsEnabled { get; set; }

    private float _roomSize = 0.5f;
    private float _damping  = 0.5f;
    private float _wet      = 0.3f;
    private float _dry      = 0.7f;

    public float RoomSize { get => _roomSize; set { _roomSize = Math.Clamp(value, 0f, 1f); Tune(); } }
    public float Damping  { get => _damping;  set { _damping  = Math.Clamp(value, 0f, 1f); Tune(); } }
    public float Wet      { get => _wet;      set { _wet      = Math.Clamp(value, 0f, 1f); _dry = 1f - _wet; } }
    public float Dry      => _dry;

    // 8 comb filter delays (Freeverb tuning values)
    private static readonly int[] CombLengths = [1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617];
    private readonly float[][] _combBuf;
    private readonly int[]     _combPos;
    private readonly float[]   _combFilt;
    private float _roomFeed, _dampA, _dampB;

    // 4 allpass delays
    private static readonly int[] AllpassLengths = [556, 441, 341, 225];
    private readonly float[][] _apBuf;
    private readonly int[]     _apPos;
    private const float AP_FEED = 0.5f;

    public ReverbEffect()
    {
        _combBuf  = CombLengths.Select(n => new float[n]).ToArray();
        _combPos  = new int[8];
        _combFilt = new float[8];
        _apBuf    = AllpassLengths.Select(n => new float[n]).ToArray();
        _apPos    = new int[4];
        Tune();
    }

    private void Tune()
    {
        _roomFeed = 0.28f + _roomSize * 0.7f;
        _dampA    = _damping * 0.4f;
        _dampB    = 1f - _dampA;
    }

    public void Process(Span<float> buf)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            float inp  = buf[i] + 1e-25f; // anti-denormal DC offset
            float outp = 0f;

            // 8 comb filters in parallel
            for (int c = 0; c < 8; c++)
            {
                float delayed = _combBuf[c][_combPos[c]];
                _combFilt[c]  = delayed * _dampB + _combFilt[c] * _dampA;
                _combBuf[c][_combPos[c]] = inp + _combFilt[c] * _roomFeed;
                _combPos[c] = (_combPos[c] + 1) % _combBuf[c].Length;
                outp += delayed;
            }

            // 4 allpass filters in series
            for (int a = 0; a < 4; a++)
            {
                float delayed = _apBuf[a][_apPos[a]];
                _apBuf[a][_apPos[a]] = outp + delayed * AP_FEED;
                _apPos[a] = (_apPos[a] + 1) % _apBuf[a].Length;
                outp = delayed - outp;
            }

            buf[i] = buf[i] * _dry + outp * _wet * 0.015f;
        }
    }

    public void Reset()
    {
        foreach (var b in _combBuf)  Array.Clear(b);
        foreach (var b in _apBuf)    Array.Clear(b);
        Array.Clear(_combFilt);
        Array.Clear(_combPos);
        Array.Clear(_apPos);
    }
}
