using Newtonsoft.Json;

namespace VybeShift.Core.Presets;

public class VybePreset
{
    public string Name            { get; set; } = "Custom";
    public string Icon            { get; set; } = "🎙";
    // Pitch
    public bool  PitchEnabled    { get; set; } = false;
    public float PitchSemitones  { get; set; } = 0f;
    // Formant
    public bool  FormantEnabled  { get; set; } = false;
    public float FormantShift    { get; set; } = 1.0f;
    // Reverb
    public bool  ReverbEnabled   { get; set; } = false;
    public float ReverbRoomSize  { get; set; } = 0.5f;
    public float ReverbDamping   { get; set; } = 0.5f;
    public float ReverbWet       { get; set; } = 0.25f;
    // Robot
    public bool  RobotEnabled    { get; set; } = false;
    // EQ
    public bool  EqEnabled       { get; set; } = false;
    public float EqLowDb         { get; set; } = 0f;
    public float EqMidDb         { get; set; } = 0f;
    public float EqHighDb        { get; set; } = 0f;
    // Distortion
    public bool  DistortEnabled  { get; set; } = false;
    public float DistortDrive    { get; set; } = 0.3f;
    // Chorus
    public bool  ChorusEnabled   { get; set; } = false;
    public float ChorusRate      { get; set; } = 1.5f;
    public float ChorusDepth     { get; set; } = 0.3f;
    // Compressor
    public bool  CompressEnabled  { get; set; } = true;
    public float CompressThreshDb { get; set; } = -18f;
    public float CompressRatio    { get; set; } = 4f;
    // Noise Gate
    public bool  GateEnabled      { get; set; } = true;
    public float GateThresholdDb  { get; set; } = -45f;
    // Gain
    public float InputGain        { get; set; } = 1.0f;
}

public sealed class PresetManager
{
    private readonly string _folder;
    private readonly List<VybePreset> _presets = [];

    public IReadOnlyList<VybePreset> Presets => _presets;
    public event Action? PresetsChanged;

    public PresetManager(string presetFolder)
    {
        _folder = presetFolder;
        Directory.CreateDirectory(_folder);
        LoadBuiltIns();
        LoadFromDisk();
    }

    public void Save(VybePreset preset)
    {
        string path = Path.Combine(_folder, $"{SanitizeName(preset.Name)}.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(preset, Formatting.Indented));
        if (!_presets.Any(p => p.Name == preset.Name)) _presets.Add(preset);
        PresetsChanged?.Invoke();
    }

    public void Delete(VybePreset preset)
    {
        string path = Path.Combine(_folder, $"{SanitizeName(preset.Name)}.json");
        if (File.Exists(path)) File.Delete(path);
        _presets.Remove(preset);
        PresetsChanged?.Invoke();
    }

    private void LoadFromDisk()
    {
        var builtInNames = _presets.Select(p => p.Name).ToHashSet();

        foreach (var file in Directory.GetFiles(_folder, "*.json"))
        {
            try
            {
                var p = JsonConvert.DeserializeObject<VybePreset>(File.ReadAllText(file));
                if (p != null)
                {
                    // If it's a built-in preset, overwrite the disk version with the new hardcoded version
                    if (builtInNames.Contains(p.Name))
                    {
                        var hardcoded = _presets.First(x => x.Name == p.Name);
                        File.WriteAllText(file, JsonConvert.SerializeObject(hardcoded, Formatting.Indented));
                    }
                    else if (!_presets.Any(x => x.Name == p.Name))
                    {
                        _presets.Add(p);
                    }
                }
            }
            catch { }
        }
        
        // Ensure all built-ins exist on disk
        foreach (var builtIn in _presets.Where(p => builtInNames.Contains(p.Name)))
        {
            string path = Path.Combine(_folder, $"{SanitizeName(builtIn.Name)}.json");
            if (!File.Exists(path))
               File.WriteAllText(path, JsonConvert.SerializeObject(builtIn, Formatting.Indented));
        }
    }

    private void LoadBuiltIns()
    {
        _presets.AddRange(new[]
        {
            new VybePreset { Name = "Normal",   Icon = "🎙",  PitchEnabled = false, GateEnabled = true },

            new VybePreset { Name = "Deep Male", Icon = "🎸",
                PitchEnabled = true,  PitchSemitones = -4f,
                FormantEnabled = true, FormantShift   = 0.82f,
                EqEnabled = true, EqLowDb = 3f, EqHighDb = -2f,
                CompressEnabled = true },

            new VybePreset { Name = "Female", Icon = "🌸",
                PitchEnabled   = true,  PitchSemitones = +4f,     // subtle shift instead of chipmunk
                FormantEnabled = true,  FormantShift   = 1.15f,   // +15% formant
                EqEnabled      = true,  EqLowDb = -3f, EqMidDb = 2f, EqHighDb = 2f, // shape presence
                CompressEnabled = true, CompressRatio = 2f,
                ChorusEnabled  = true,  ChorusRate = 1.0f, ChorusDepth = 0.1f },

            new VybePreset { Name = "Robot", Icon = "🤖",
                RobotEnabled  = true,
                PitchEnabled  = true,  PitchSemitones = -2f,
                ReverbEnabled = true,  ReverbRoomSize = 0.5f, ReverbWet = 0.15f },

            new VybePreset { Name = "Demon", Icon = "😈",
                PitchEnabled   = true,  PitchSemitones  = -9f,
                FormantEnabled = true,  FormantShift     = 0.75f,
                DistortEnabled = true,  DistortDrive     = 0.45f,
                ReverbEnabled  = true,  ReverbRoomSize   = 0.7f, ReverbWet = 0.3f,
                GateEnabled    = true,  GateThresholdDb  = -65f },

            new VybePreset { Name = "Anime", Icon = "🌟",
                PitchEnabled   = true,  PitchSemitones = +5f,
                FormantEnabled = true,  FormantShift   = 1.10f,
                ChorusEnabled  = true,  ChorusRate = 2.0f,  ChorusDepth = 0.3f,
                ReverbEnabled  = true,  ReverbRoomSize = 0.35f, ReverbWet = 0.15f },

            new VybePreset { Name = "Cave Echo", Icon = "🏔",
                ReverbEnabled = true,  ReverbRoomSize = 0.9f, ReverbDamping = 0.3f, ReverbWet = 0.5f,
                PitchEnabled  = false },

            new VybePreset { Name = "Chipmunk", Icon = "🐿",
                PitchEnabled   = true, PitchSemitones = +12f,
                FormantEnabled = false },
        });
    }

    private static string SanitizeName(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
