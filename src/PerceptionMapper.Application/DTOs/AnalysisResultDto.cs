namespace PerceptionMapper.Application.DTOs;

public class AnalysisResultDto
{
    public int Language { get; set; }
    public int DominantTone { get; set; }
    public Dictionary<int, int> ToneScores { get; set; } = new();
    public bool IsToneAmbiguous { get; set; }
    public int ToneConfidencePercent { get; set; }
    public bool UsedVoiceTone { get; set; }
    public List<string> FlaggedBiasPhrases { get; set; } = new();
    public List<BiasFlagDto> BiasFlags { get; set; } = new();
    public List<string> FillerWords { get; set; } = new();
    public string FeedbackMessage { get; set; } = string.Empty;
    public List<string> RephrasingSuggestions { get; set; } = new();
    public List<SpeakingMomentDto> SpeakingMoments { get; set; } = new();
    public string Scenario { get; set; } = "General";
    public int CommunicationScore { get; set; }
    public string ScoreLabel { get; set; } = string.Empty;
    public string ScenarioTip { get; set; } = string.Empty;
    public List<string> TargetToneRephrases { get; set; } = new();
}
