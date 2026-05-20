namespace PerceptionMapper.Domain.Entities;

public class PracticeSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Scenario { get; set; } = "General";
    public string DominantTone { get; set; } = string.Empty;
    public int CommunicationScore { get; set; }
    public string DetectedLanguage { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public int MomentCount { get; set; }
    public string InputPreview { get; set; } = string.Empty;
    public string MomentsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
