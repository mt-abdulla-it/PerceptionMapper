using System.IO;
using System.Windows;
using System.Windows.Threading;
using NAudio.Wave;
using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Services;
using PerceptionMapper.ML;

namespace PerceptionMapper.Desktop.Services;

/// <summary>Real-time streaming voice analysis while the user is speaking.</summary>
public sealed class RealtimeVoiceCoachService : IDisposable
{
    private readonly HybridEmotionAnalyzer _emotion = new();
    private readonly List<SpeakingMomentDto> _liveMoments = new();
    private readonly object _lock = new();

    private WaveInEvent? _waveIn;
    private readonly List<float> _buffer = new();
    private DispatcherTimer? _timer;
    private double _elapsedSeconds;
    private int _sampleRate = 16000;
    private bool _running;

    public bool IsRunning => _running;
    public bool IsMlActive => _emotion.IsMlEnabled;

    public event EventHandler<SpeakingMomentDto>? MomentDetected;
    public event EventHandler<string>? StatusChanged;

    public void Start()
    {
        Stop();
        _running = true;
        _elapsedSeconds = 0;
        lock (_lock) { _liveMoments.Clear(); _buffer.Clear(); }

        _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(_sampleRate, 1) };
        _waveIn.DataAvailable += OnData;
        _waveIn.StartRecording();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => AnalyzeLatestWindow();
        _timer.Start();

        var mode = _emotion.IsMlEnabled ? "ML emotion + rules" : "rule-based (add emotion_model.onnx for ML)";
        StatusChanged?.Invoke(this, $"Live coach started ({mode})...");
    }

    public void Stop()
    {
        _running = false;
        _timer?.Stop();
        _timer = null;

        if (_waveIn is not null)
        {
            _waveIn.StopRecording();
            _waveIn.DataAvailable -= OnData;
            _waveIn.Dispose();
            _waveIn = null;
        }
    }

    public IReadOnlyList<SpeakingMomentDto> GetCapturedMoments()
    {
        lock (_lock)
            return _liveMoments.ToList();
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        var sampleCount = e.BytesRecorded / 2;
        lock (_lock)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var sample = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;
                _buffer.Add(sample);
            }

            var maxSamples = _sampleRate * 30;
            if (_buffer.Count > maxSamples)
                _buffer.RemoveRange(0, _buffer.Count - maxSamples);
        }
    }

    private void AnalyzeLatestWindow()
    {
        if (!_running) return;

        float[] window;
        lock (_lock)
        {
            var windowSize = (int)(3 * _sampleRate);
            if (_buffer.Count < _sampleRate) return;
            var take = Math.Min(windowSize, _buffer.Count);
            window = _buffer.Skip(_buffer.Count - take).ToArray();
            _elapsedSeconds += 2;
        }

        var segments = BuildQuickSegments(window, _elapsedSeconds);
        var mlWindows = _emotion.IsMlEnabled
            ? new List<(double, float[])> { (_elapsedSeconds, window) }
            : null;

        var moments = _emotion.AnalyzeSegments(segments, mlWindows);
        foreach (var moment in moments)
        {
            lock (_lock)
            {
                if (_liveMoments.Any(m => m.Message == moment.Message)) continue;
                _liveMoments.Add(moment);
            }

            System.Windows.Application.Current?.Dispatcher.Invoke(() => MomentDetected?.Invoke(this, moment));
        }
    }

    private static List<AudioSegmentDto> BuildQuickSegments(float[] window, double atSec)
    {
        var rms = Math.Sqrt(window.Select(s => s * s).Average());
        var seg = new AudioSegmentDto
        {
            StartSeconds = Math.Max(0, atSec - 3),
            EndSeconds = atSec,
            Energy = Math.Clamp(rms * 4, 0, 1),
            EnergyVariation = 0.3,
            PitchVariation = 0.35,
            WordsPerMinute = 120
        };
        return [seg];
    }

    public void Dispose()
    {
        Stop();
        _emotion.Dispose();
    }
}
