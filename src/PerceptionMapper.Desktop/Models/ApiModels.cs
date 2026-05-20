using PerceptionMapper.Application.DTOs;

namespace PerceptionMapper.Desktop.Models;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? UserId { get; set; }
    public string? Username { get; set; }
}

public class AnalysisResponse
{
    public int Language { get; set; }
    public int DominantTone { get; set; }
    public Dictionary<int, int>? ToneScores { get; set; }
    public bool IsToneAmbiguous { get; set; }
    public int ToneConfidencePercent { get; set; }
    public bool UsedVoiceTone { get; set; }
    public List<string>? FlaggedBiasPhrases { get; set; }
    public List<BiasFlagDto>? BiasFlags { get; set; }
    public List<string>? FillerWords { get; set; }
    public string FeedbackMessage { get; set; } = "";
    public List<string>? RephrasingSuggestions { get; set; }
    public List<SpeakingMomentDto>? SpeakingMoments { get; set; }
    public string Scenario { get; set; } = "General";
    public int CommunicationScore { get; set; }
    public string ScoreLabel { get; set; } = "";
    public string ScenarioTip { get; set; } = "";
    public List<string>? TargetToneRephrases { get; set; }
}

public class HistoryItem
{
    public int Id { get; set; }
    public string InputText { get; set; } = "";
    public string DetectedLanguage { get; set; } = "";
    public string DominantTone { get; set; } = "";
    public string FeedbackMessage { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}

public class PracticeSessionItem
{
    public int Id { get; set; }
    public string Scenario { get; set; } = "";
    public string DominantTone { get; set; } = "";
    public int CommunicationScore { get; set; }
    public string DetectedLanguage { get; set; } = "";
    public double DurationSeconds { get; set; }
    public int MomentCount { get; set; }
    public string InputPreview { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Summary => $"{CreatedAt:g} | {Scenario} | Score {CommunicationScore} | {DominantTone}";
}

public static class Session
{
    public static int UserId { get; set; }
    public static string Username { get; set; } = "";
}
