using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PerceptionMapper.Application.DTOs;

namespace PerceptionMapper.ML;

/// <summary>
/// Offline speech emotion recognition via ONNX (when model file is present).
/// </summary>
public sealed class OnnxSpeechEmotionAnalyzer : IDisposable
{
    private readonly InferenceSession? _session;
    private readonly string[] _labels;
    private readonly int _inputSamples;
    private readonly string _inputName;
    private readonly string _outputName;

    public bool IsAvailable => _session is not null;

    public OnnxSpeechEmotionAnalyzer(string? modelsDirectory = null)
    {
        modelsDirectory ??= FindModelsDirectory();
        var modelPath = Path.Combine(modelsDirectory, "emotion_model.onnx");
        var labelPath = Path.Combine(modelsDirectory, "emotion_labels.json");

        _labels = LoadLabels(labelPath);
        _inputSamples = 48000;
        _inputName = "input";
        _outputName = "output";

        if (!File.Exists(modelPath))
            return;

        try
        {
            _session = new InferenceSession(modelPath);
            _inputName = _session.InputMetadata.Keys.First();
            _outputName = _session.OutputMetadata.Keys.First();
        }
        catch (Exception)
        {
            // Model incompatible or missing — rule-based coaching still works.
        }
    }

    public EmotionPrediction? Predict(float[] samples16k)
    {
        if (_session is null || samples16k.Length == 0)
            return null;

        var input = PadOrTrim(samples16k, _inputSamples);
        var tensor = new DenseTensor<float>(input, new[] { 1, _inputSamples });
        using var results = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(_inputName, tensor) });
        var output = results.First(v => v.Name == _outputName).AsEnumerable<float>().ToArray();
        if (output.Length == 0)
            return null;

        var probs = Softmax(output);
        var labelCount = Math.Min(probs.Length, _labels.Length);
        var map = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var bestIdx = 0;
        for (var i = 0; i < labelCount; i++)
        {
            map[_labels[i]] = probs[i];
            if (probs[i] > probs[bestIdx])
                bestIdx = i;
        }

        return new EmotionPrediction
        {
            TopEmotion = _labels[bestIdx],
            TopScore = probs[bestIdx],
            Scores = map
        };
    }

    public List<SpeakingMomentDto> ToCoachingMoments(EmotionPrediction prediction, double atSeconds)
    {
        var time = FormatTime(atSeconds);
        var issue = MapEmotionToIssue(prediction.TopEmotion);
        if (issue is null || prediction.TopScore < 0.45)
            return new List<SpeakingMomentDto>();

        var message = issue switch
        {
            "Harsh" => $"{time}: Your tone sounds slightly harsh here — soften your delivery.",
            "Nervous" => $"{time}: You may sound nervous at this point — slow down and breathe.",
            "LowConfidence" => $"{time}: Low confidence detected — project your voice more clearly.",
            _ => $"{time}: Delivery note — check pacing and clarity."
        };

        return
        [
            new SpeakingMomentDto
            {
                StartSeconds = atSeconds,
                EndSeconds = atSeconds + 2.5,
                Issue = issue,
                Message = message
            }
        ];
    }

    public static string? MapEmotionToIssue(string emotion) => emotion.ToLowerInvariant() switch
    {
        "angry" or "disgust" => "Harsh",
        "fearful" or "fear" or "surprised" => "Nervous",
        "sad" => "LowConfidence",
        "happy" or "calm" or "neutral" => null,
        _ => null
    };

    private static string[] LoadLabels(string labelPath)
    {
        try
        {
            if (!File.Exists(labelPath))
                return DefaultLabels();

            var json = JsonSerializer.Deserialize<LabelConfig>(File.ReadAllText(labelPath));
            return json?.Labels?.Length > 0 ? json.Labels : DefaultLabels();
        }
        catch
        {
            return DefaultLabels();
        }
    }

    private static string[] DefaultLabels() =>
        ["neutral", "calm", "happy", "sad", "angry", "fearful", "disgust", "surprised"];

    private static string FindModelsDirectory()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(dir, "models");
            if (Directory.Exists(candidate))
                return candidate;
            var parent = Path.Combine(dir, "..", "models");
            if (Directory.Exists(parent))
                return Path.GetFullPath(parent);
            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }

        return Path.Combine(Environment.CurrentDirectory, "models");
    }

    private static float[] PadOrTrim(float[] input, int size)
    {
        if (input.Length == size)
            return input;
        var result = new float[size];
        Array.Copy(input, result, Math.Min(input.Length, size));
        return result;
    }

    private static float[] Softmax(float[] logits)
    {
        var max = logits.Max();
        var exp = logits.Select(v => Math.Exp(v - max)).ToArray();
        var sum = exp.Sum();
        return exp.Select(v => (float)(v / sum)).ToArray();
    }

    private static string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}" : $"{(int)ts.TotalSeconds}s";
    }

    public void Dispose() => _session?.Dispose();

    private sealed class LabelConfig
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
    }
}
