using System.Globalization;
using System.IO;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows;
using NAudio.Wave;
using PerceptionMapper.Desktop.Models;

namespace PerceptionMapper.Desktop.Services;

public sealed class SpeechService : IDisposable
{
    private SpeechSynthesizer? _synthesizer;
    private CancellationTokenSource? _listenCts;
    private Task? _listenTask;
    private bool _isListening;

    public bool IsListening => _isListening;
    public static bool HasRecognizer => SpeechRecognitionEngine.InstalledRecognizers().Count > 0;

    public event EventHandler<SpeechCapturedEventArgs>? SpeechCaptured;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? ListeningStopped;

    public static IReadOnlyList<VoiceLanguageOption> LanguageOptions { get; } =
    [
        new("English", "en-US"),
        new("Tamil", "ta-IN"),
        new("Sinhala", "si-LK")
    ];

    public static string GetDiagnostics()
    {
        var installed = SpeechRecognitionEngine.InstalledRecognizers();
        if (installed.Count == 0)
        {
            return "No speech recognizers installed.\n" +
                   "Fix: Settings → Time & language → Language → English → Options → add Speech recognition.\n" +
                   "Then: Settings → Privacy → Microphone → allow desktop apps.";
        }

        return "Recognizers: " + string.Join(", ", installed.Select(r => r.Culture.Name));
    }

    public void StartListening(string cultureName)
    {
        if (_isListening) return;

        if (!HasRecognizer)
        {
            RaiseError(GetDiagnostics());
            return;
        }

        _listenCts = new CancellationTokenSource();
        _isListening = true;
        _listenTask = ListenWorkerAsync(cultureName, _listenCts.Token);
    }

    private async Task ListenWorkerAsync(string cultureName, CancellationToken cancellationToken)
    {
        try
        {
            var result = await Task.Run(() => RecognizeBlocking(cultureName, cancellationToken));

            if (cancellationToken.IsCancellationRequested)
                return;

            if (string.IsNullOrWhiteSpace(result.Text))
            {
                TryDeleteFile(result.AudioPath);
                RaiseError(
                    "No speech detected.\n\n" +
                    "1. Click Start listening\n" +
                    "2. Wait for the beep\n" +
                    "3. Speak clearly for 5–10 seconds\n" +
                    "4. Use English unless that speech pack is installed\n\n" +
                    GetDiagnostics());
                return;
            }

            Dispatch(() => SpeechCaptured?.Invoke(this, new SpeechCapturedEventArgs
            {
                Text = result.Text!.Trim(),
                AudioPath = result.AudioPath
            }));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
                RaiseError($"Voice input failed:\n{ex.Message}\n\n{GetDiagnostics()}");
        }
        finally
        {
            _isListening = false;
            _listenCts?.Dispose();
            _listenCts = null;
            Dispatch(() => ListeningStopped?.Invoke(this, EventArgs.Empty));
        }
    }

    private sealed record RecognizeResult(string? Text, string? AudioPath);

    private static RecognizeResult RecognizeBlocking(string cultureName, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return new RecognizeResult(null, null);

        var info = FindRecognizer(cultureName)
                   ?? throw new InvalidOperationException("No speech recognizer for " + cultureName);

        var capturePath = Path.Combine(Path.GetTempPath(), $"pm_voice_{Guid.NewGuid():N}.wav");
        WaveInEvent? waveIn = null;
        WaveFileWriter? writer = null;

        string? recognized = null;
        Exception? failure = null;
        using var done = new ManualResetEventSlim(false);

        try
        {
            waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
            writer = new WaveFileWriter(capturePath, waveIn.WaveFormat);
            waveIn.DataAvailable += (_, e) => writer.Write(e.Buffer, 0, e.BytesRecorded);
            waveIn.StartRecording();
        }
        catch
        {
            capturePath = "";
        }

        var thread = new Thread(() =>
        {
            try
            {
                using var engine = new SpeechRecognitionEngine(info);
                engine.SetInputToDefaultAudioDevice();
                engine.LoadGrammar(new DictationGrammar());
                engine.BabbleTimeout = TimeSpan.FromSeconds(0);
                engine.InitialSilenceTimeout = TimeSpan.FromSeconds(12);
                engine.EndSilenceTimeout = TimeSpan.FromSeconds(2);
                engine.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(2.5);

                engine.SpeechRecognized += (_, e) =>
                {
                    recognized = e.Result?.Text;
                    done.Set();
                };

                engine.RecognizeCompleted += (_, e) =>
                {
                    if (e.Error is not null)
                        failure = e.Error;
                    done.Set();
                };

                engine.RecognizeAsync(RecognizeMode.Single);

                while (!done.Wait(250))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try { engine.RecognizeAsyncCancel(); } catch { /* ignore */ }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                failure = ex;
                done.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(35));

        try
        {
            waveIn?.StopRecording();
        }
        catch { /* ignore */ }

        writer?.Dispose();
        waveIn?.Dispose();

        if (cancellationToken.IsCancellationRequested)
        {
            TryDeleteFile(string.IsNullOrEmpty(capturePath) ? null : capturePath);
            return new RecognizeResult(null, null);
        }

        if (failure is not null)
            throw failure;

        var audio = string.IsNullOrEmpty(capturePath) || !File.Exists(capturePath) ? null : capturePath;
        return new RecognizeResult(recognized, audio);
    }

    private static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { File.Delete(path); } catch { /* ignore */ }
    }

    private static RecognizerInfo? FindRecognizer(string cultureName)
    {
        var installed = SpeechRecognitionEngine.InstalledRecognizers();
        if (installed.Count == 0)
            return null;

        var culture = CultureInfo.GetCultureInfo(cultureName);
        return installed.FirstOrDefault(r =>
                   r.Culture.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase))
               ?? installed.FirstOrDefault(r =>
                   r.Culture.TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName,
                       StringComparison.OrdinalIgnoreCase))
               ?? installed.FirstOrDefault(r =>
                   r.Culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
               ?? installed[0];
    }

    public void StopListening() => _listenCts?.Cancel();

    public void Speak(string text, string cultureName)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        StopSpeaking();

        try
        {
            _synthesizer = new SpeechSynthesizer();
            TrySelectVoice(cultureName);
            _synthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            RaiseError($"Text-to-speech failed:\n{ex.Message}");
        }
    }

    public void StopSpeaking()
    {
        if (_synthesizer is null) return;
        try
        {
            _synthesizer.SpeakAsyncCancelAll();
            _synthesizer.Dispose();
        }
        catch { /* ignore */ }
        _synthesizer = null;
    }

    private void TrySelectVoice(string cultureName)
    {
        if (_synthesizer is null) return;

        var culture = CultureInfo.GetCultureInfo(cultureName);
        var voice = _synthesizer.GetInstalledVoices()
            .Select(v => v.VoiceInfo)
            .FirstOrDefault(v =>
                v.Culture.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase)
                || v.Culture.TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName,
                    StringComparison.OrdinalIgnoreCase));

        if (voice is not null)
            _synthesizer.SelectVoice(voice.Name);
        else
            _synthesizer.SelectVoiceByHints(VoiceGender.Neutral, VoiceAge.Adult);
    }

    private void RaiseError(string message) =>
        Dispatch(() => ErrorOccurred?.Invoke(this, message));

    private static void Dispatch(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
            action();
        else
            System.Windows.Application.Current?.Dispatcher?.Invoke(action);
    }

    public void Dispose()
    {
        StopListening();
        StopSpeaking();
        try { _listenTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
    }
}

public record VoiceLanguageOption(string DisplayName, string CultureName);
