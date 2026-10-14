namespace VybeShift.Core.DSP;

/// <summary>
/// Ordered, stackable DSP effect chain. Thread-safe for parameter changes.
/// Process() is called on the DSP thread — no locks, no allocs.
/// </summary>
public sealed class DSPChain
{
    private readonly List<IAudioEffect> _effects = [];

    public IReadOnlyList<IAudioEffect> Effects => _effects;

    public void Add(IAudioEffect effect)    => _effects.Add(effect);
    public void Remove(IAudioEffect effect) => _effects.Remove(effect);
    public void Clear()                     => _effects.Clear();

    /// <summary>Run all enabled effects in order, in-place.</summary>
    public void Process(Span<float> buffer)
    {
        foreach (var fx in _effects)
        {
            if (fx.IsEnabled)
            {
                try { fx.Process(buffer); }
                catch { /* never crash the audio thread */ }
            }
        }
    }

    public void Reset()
    {
        foreach (var fx in _effects) fx.Reset();
    }

    // ── Factory: build the default VybeShift chain ────────────────────────

    public static DSPChain CreateDefault()
    {
        var chain = new DSPChain();
        chain.Add(new NoiseGateEffect   { IsEnabled = true  });
        chain.Add(new PitchShiftEffect  { IsEnabled = false });
        chain.Add(new FormantShiftEffect{ IsEnabled = false });
        chain.Add(new EQEffect          { IsEnabled = false });
        chain.Add(new RobotEffect       { IsEnabled = false });
        chain.Add(new DistortionEffect  { IsEnabled = false });
        chain.Add(new ChorusEffect      { IsEnabled = false });
        chain.Add(new ReverbEffect      { IsEnabled = false });
        chain.Add(new CompressorEffect  { IsEnabled = true  });
        chain.Add(new LimiterEffect     { IsEnabled = true  });
        return chain;
    }
}
