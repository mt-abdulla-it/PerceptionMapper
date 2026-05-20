namespace PerceptionMapper.Api.Models;

public class PracticeSessionDto
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
}
