using PerceptionMapper.Application.DTOs;

namespace PerceptionMapper.Api.Models;

public record AnalyzeRequest(
    string Text,
    int UserId,
    AudioProsodyMetricsDto? VoiceMetrics = null,
    string? Scenario = null,
    double DurationSeconds = 0,
    int? RephraseTargetTone = null,
    int? DeclaredInputTone = null);
