using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using VybeShift.Core.DSP;

namespace VybeShift.Core.Audio;

/// <summary>
/// Main audio engine.
/// Thread 1 (TimeCritical): WASAPI capture → normalizer → ring buffer
/// Thread 2 (Highest):      ring buffer → DSP chain → output buffer
/// Output:                  WASAPI shared write to VB-Cable or default output
/// </summary>
public sealed class AudioEngine : IDisposable
{
    public AudioEngineConfig Config  { get; } = new();
    public AudioMetrics      Metrics { get; } = new();
    public DSPChain          Chain   { get; } = DSPChain.CreateDefault();

    public event Action<AudioEngineState>? StateChanged;
    public event Action<string>?           OnError;
    public event Action?                   OnDeviceDisconnected;

    public AudioEngineState State { get; private set; } = AudioEngineState.Stopped;

    // ── Constants ─────────────────────────────────────────────────────────
    private const int SAMPLE_RATE  = 48000;
    private const int RING_SIZE    = SAMPLE_RATE * 2;  // 2s safety margin
    private const int SCRATCH_SIZE = 4096;

    // ── Audio objects ─────────────────────────────────────────────────────
    private WasapiCapture?        _capture;
    private WasapiOut?            _wasapiOut;
    private BufferedWaveProvider? _playback;
    private WasapiOut?            _loopbackOut;
    private BufferedWaveProvider? _loopbackPlayback;
    private InputNormalizer?      _normalizer;

    // ── Threading ─────────────────────────────────────────────────────────
    private readonly RingBuffer _captureRing = new(RING_SIZE);
    private readonly float[]    _scratch     = new float[SCRATCH_SIZE];
    private Thread?             _dspThread;
    private volatile bool       _running     = false;
    private volatile float      _inputGain   = 1.0f;

    // ── Metrics ───────────────────────────────────────────────────────────
    private readonly Stopwatch _sw = new();
    private int _underruns = 0;

    // ── Soundboard mixer ──────────────────────────────────────────────────
    private BufferedWaveProvider? _soundboardSource;

    // ── Public API ────────────────────────────────────────────────────────

    public void Start(DeviceManager devices)
    {
        if (State == AudioEngineState.Running) return;
        try
        {
            SetupCapture(devices);
            SetupOutput(devices);
            _running = true;
            StartDSPThread();
            _capture!.StartRecording();
            _wasapiOut!.Play();
            _loopbackOut?.Play();
            SetState(AudioEngineState.Running);
        }
        catch (Exception ex)
        {
            SetState(AudioEngineState.Error);
            Metrics.Status = ex.Message;
            OnError?.Invoke(ex.Message);
        }
    }

    public void Stop()
    {
        _running = false;
        _dspThread?.Join(600);
        SafeStop(_capture);
        SafeStop(_wasapiOut);
        SafeStop(_loopbackOut);
        _captureRing.Clear();
        Chain.Reset();
        SetState(AudioEngineState.Stopped);
    }

    public void Restart(DeviceManager devices) { Stop(); Start(devices); }

    public void SetInputGain(float g)      => _inputGain = Math.Clamp(g, 0f, 4f);
    public void SetPassthrough(bool on)    => Config.Passthrough = on;

    // ── Soundboard: mix an MP3 sample into the output ─────────────────────
    public void PlaySoundboardClip(string path)
    {
        try
        {
            var reader = new NAudio.Wave.Mp3FileReader(path);
            var resampled = new MediaFoundationResampler(reader,
                WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, 1));
            _soundboardSource = new BufferedWaveProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, 1));
            // Read ahead into buffer
            var buf = new byte[resampled.WaveFormat.AverageBytesPerSecond * 10];
            int read = resampled.Read(buf, 0, buf.Length);
            _soundboardSource.AddSamples(buf, 0, read);
        }
        catch { /* silent fail — never crash audio thread */ }
    }

    // ── Setup ─────────────────────────────────────────────────────────────

    private void SetupCapture(DeviceManager devices)
    {
        var device = string.IsNullOrEmpty(Config.InputDeviceId)
            ? devices.DefaultInput()
            : devices.GetById(Config.InputDeviceId);

        if (device == null) throw new Exception("No input device available.");

        _normalizer = new InputNormalizer();
        _capture    = new WasapiCapture(device, false, Config.BufferMs);
        _capture.DataAvailable   += OnCapture;
        _capture.RecordingStopped += OnCaptureStopped;
    }

    private void SetupOutput(DeviceManager devices)
    {
        var device = devices.FindVBCable()
            ?? (string.IsNullOrEmpty(Config.OutputDeviceId)
                ? devices.DefaultOutput()
                : devices.GetById(Config.OutputDeviceId));

        if (device == null) throw new Exception("No output device available.");

        var fmt = WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, 1);
        _playback  = new BufferedWaveProvider(fmt)
        {
            BufferDuration          = TimeSpan.FromSeconds(1),
            DiscardOnBufferOverflow = true
        };
        _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, true, 20);
        _wasapiOut.Init(_playback);

        // HearMyself Loopback
        if (Config.HearMyself)
        {
            var defOut = devices.DefaultOutput();
            // Dont loopback into the same device we are already outputting to
            if (defOut != null && defOut.ID != device.ID)
            {
                try 
                {
                    _loopbackPlayback = new BufferedWaveProvider(fmt)
                    {
                        BufferDuration          = TimeSpan.FromSeconds(1),
                        DiscardOnBufferOverflow = true
                    };
                    _loopbackOut = new WasapiOut(defOut, AudioClientShareMode.Shared, true, 20);
                    _loopbackOut.Init(_loopbackPlayback);
                } 
                catch { _loopbackOut = null; _loopbackPlayback = null; }
            }
        }
        else
        {
            _loopbackOut = null;
            _loopbackPlayback = null;
        }
    }

    // ── Capture callback (WASAPI capture thread) ──────────────────────────

    private void OnCapture(object? _, WaveInEventArgs e)
    {
        if (!_running || e.BytesRecorded == 0) return;
        _sw.Restart();

        var samples = _normalizer!.Normalize(e.Buffer, e.BytesRecorded, _capture!.WaveFormat);
        float gain = _inputGain;
        float lvl  = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= gain;
            lvl = Math.Max(lvl, MathF.Abs(samples[i]));
        }
        Metrics.InputLevel = lvl;

        if (_captureRing.Write(samples) < samples.Length)
            Interlocked.Increment(ref _underruns);
    }

    private void OnCaptureStopped(object? _, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            OnError?.Invoke(e.Exception.Message);
            OnDeviceDisconnected?.Invoke();
        }
    }

    // ── DSP Thread ────────────────────────────────────────────────────────

    private void StartDSPThread()
    {
        _dspThread = new Thread(DSPLoop)
        {
            Name         = "VybeShift.DSP",
            Priority     = ThreadPriority.Highest,
            IsBackground = true
        };
        _dspThread.Start();
    }

    private void DSPLoop()
    {
        int chunkSize = SAMPLE_RATE * Config.BufferMs / 1000;

        while (_running)
        {
            if (_captureRing.Available < chunkSize) { Thread.Sleep(1); continue; }

            var chunk = _scratch.AsSpan(0, Math.Min(chunkSize, SCRATCH_SIZE));
            int read  = _captureRing.Read(chunk);
            if (read == 0) continue;

            var work = chunk[..read];

            if (!Config.Passthrough) Chain.Process(work);

            // Mix soundboard if active
            if (_soundboardSource?.BufferedBytes > 0)
            {
                var sbBuf = new byte[read * 4];
                int sbRead = _soundboardSource.Read(sbBuf, 0, sbBuf.Length);
                for (int i = 0; i < read && i*4+3 < sbRead; i++)
                    work[i] = Math.Clamp(work[i] + BitConverter.ToSingle(sbBuf, i*4) * 0.4f, -1f, 1f);
            }

            // Track output level
            float lvl = 0f;
            foreach (var s in work) lvl = Math.Max(lvl, MathF.Abs(s));
            Metrics.OutputLevel  = lvl;
            Metrics.LatencyMs    = _sw.Elapsed.TotalMilliseconds;
            Metrics.Underruns    = _underruns;

            // Float span → byte array → BufferedWaveProvider
            var bytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(work).ToArray();
            _playback?.AddSamples(bytes, 0, bytes.Length);
            _loopbackPlayback?.AddSamples(bytes, 0, bytes.Length);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SetState(AudioEngineState s) { State = s; StateChanged?.Invoke(s); Metrics.Status = s.ToString(); }
    private static void SafeStop(object? obj) { try { if (obj is WasapiCapture c) c.StopRecording(); else if (obj is WasapiOut o) o.Stop(); } catch { } }

    public void Dispose() { Stop(); _capture?.Dispose(); _wasapiOut?.Dispose(); _loopbackOut?.Dispose(); }
}
