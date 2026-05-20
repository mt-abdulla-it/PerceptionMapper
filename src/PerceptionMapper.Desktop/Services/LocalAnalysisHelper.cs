using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Services;
using PerceptionMapper.Desktop.Models;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Desktop.Services;

public static class LocalAnalysisHelper
{
    private static readonly AnalysisService Analyzer = new();

    public static AnalysisResponse Analyze(
        string text,
        AudioProsodyMetricsDto? voiceMetrics = null,
        string? scenario = null,
        int? rephraseTargetTone = null,
        int? declaredInputTone = null)
    {
        EmotionalTone? target = rephraseTargetTone is >= 0 and <= 4
            ? (EmotionalTone)rephraseTargetTone.Value
            : null;
        EmotionalTone? declared = declaredInputTone is >= 0 and <= 4
            ? (EmotionalTone)declaredInputTone.Value
            : null;
        var dto = Analyzer.Analyze(text, voiceMetrics, scenario, target, declared);
        return Map(dto);
    }

    public static AnalysisResponse Map(AnalysisResultDto dto) => new()
    {
        Language = dto.Language,
        DominantTone = dto.DominantTone,
        ToneScores = dto.ToneScores,
        IsToneAmbiguous = dto.IsToneAmbiguous,
        ToneConfidencePercent = dto.ToneConfidencePercent,
        UsedVoiceTone = dto.UsedVoiceTone,
        FlaggedBiasPhrases = dto.FlaggedBiasPhrases,
        BiasFlags = dto.BiasFlags,
        FillerWords = dto.FillerWords,
        FeedbackMessage = dto.FeedbackMessage,
        RephrasingSuggestions = dto.RephrasingSuggestions,
        SpeakingMoments = dto.SpeakingMoments,
        Scenario = dto.Scenario,
        CommunicationScore = dto.CommunicationScore,
        ScoreLabel = dto.ScoreLabel,
        ScenarioTip = dto.ScenarioTip,
        TargetToneRephrases = dto.TargetToneRephrases
    };
}
