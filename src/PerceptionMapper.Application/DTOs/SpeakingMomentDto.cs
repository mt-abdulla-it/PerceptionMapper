namespace PerceptionMapper.Application.DTOs;

public class SpeakingMomentDto
{
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string Issue { get; set; } = ""; // Nervous, Harsh, LowConfidence, Hesitation
    public string Message { get; set; } = "";
}
