namespace PerceptionMapper.Application.DTOs;

/// <summary>Features extracted from the user's voice recording (offline prosody).</summary>
public class AudioProsodyMetricsDto
{
    public double AverageEnergy { get; set; }
    public double EnergyVariation { get; set; }
    public double WordsPerMinute { get; set; }
    public double PitchVariation { get; set; }
    public double DurationSeconds { get; set; }
    public List<SpeakingMomentDto> SpeakingMoments { get; set; } = new();
}
