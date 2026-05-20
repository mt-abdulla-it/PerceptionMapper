namespace PerceptionMapper.Api.Models;

public class HistoryItemDto
{
    public int Id { get; set; }
    public string InputText { get; set; } = "";
    public string DetectedLanguage { get; set; } = "";
    public string DominantTone { get; set; } = "";
    public string FeedbackMessage { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}
