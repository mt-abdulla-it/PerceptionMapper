using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Services;

namespace PerceptionMapper.ML;

/// <summary>Combines ONNX ML emotion model with rule-based segment coaching.</summary>
public sealed class HybridEmotionAnalyzer : IDisposable
{
    private readonly OnnxSpeechEmotionAnalyzer _onnx = new();

    public bool IsMlEnabled => _onnx.IsAvailable;

    public List<SpeakingMomentDto> AnalyzeSegments(
        IReadOnlyList<AudioSegmentDto> segments,
        IReadOnlyList<(double StartSec, float[] Samples)>? mlWindows = null)
    {
        var moments = SpeakingCoachService.AnalyzeSegments(segments);

        if (!IsMlEnabled || mlWindows is null)
            return moments;

        foreach (var (start, samples) in mlWindows)
        {
            var pred = _onnx.Predict(samples);
            if (pred is null) continue;
            moments.AddRange(_onnx.ToCoachingMoments(pred, start));
        }

        return moments
            .OrderBy(m => m.StartSeconds)
            .GroupBy(m => $"{m.StartSeconds:F1}:{m.Issue}")
            .Select(g => g.First())
            .Take(10)
            .ToList();
    }

    public void Dispose() => _onnx.Dispose();
}
