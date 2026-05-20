using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Interfaces;

public interface IAnalysisService
{
    AnalysisResultDto Analyze(
        string text,
        AudioProsodyMetricsDto? voiceMetrics = null,
        string? scenarioId = null,
        EmotionalTone? rephraseTarget = null,
        EmotionalTone? declaredInputTone = null);
}
