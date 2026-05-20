namespace PerceptionMapper.Domain.Entities;

public class AnalysisRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string InputText { get; set; } = string.Empty;
    public string DetectedLanguage { get; set; } = string.Empty;
    public string DominantTone { get; set; } = string.Empty;
    public string ToneScoresJson { get; set; } = "{}";
    public string FlaggedBiasJson { get; set; } = "[]";
    public string FeedbackMessage { get; set; } = string.Empty;
    public string RephrasingSuggestions { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
