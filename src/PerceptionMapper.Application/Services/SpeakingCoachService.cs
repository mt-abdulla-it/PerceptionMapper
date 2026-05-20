using PerceptionMapper.Application.DTOs;

namespace PerceptionMapper.Application.Services;

/// <summary>
/// Maps voice segments to professional coaching feedback (offline prosody rules).
/// </summary>
public static class SpeakingCoachService
{
    public static List<SpeakingMomentDto> AnalyzeSegments(IReadOnlyList<AudioSegmentDto> segments)
    {
        var moments = new List<SpeakingMomentDto>();
        if (segments.Count == 0)
            return moments;

        for (var i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            var timeLabel = FormatTime(s.StartSeconds);

            // Harsh: loud + sharp volume changes.
            if (s.Energy > 0.55 && s.EnergyVariation > 0.4)
            {
                moments.Add(new SpeakingMomentDto
                {
                    StartSeconds = s.StartSeconds,
                    EndSeconds = s.EndSeconds,
                    Issue = "Harsh",
                    Message = $"{timeLabel}: Your tone sounds slightly harsh here — try softer volume."
                });
                continue;
            }

            // Nervous: fast + uneven energy / high pitch movement.
            if (s.WordsPerMinute > 155 && (s.EnergyVariation > 0.35 || s.PitchVariation > 0.5))
            {
                moments.Add(new SpeakingMomentDto
                {
                    StartSeconds = s.StartSeconds,
                    EndSeconds = s.EndSeconds,
                    Issue = "Nervous",
                    Message = $"{timeLabel}: You may sound nervous at this point — slow down and breathe."
                });
                continue;
            }

            // Low confidence: quiet + slow.
            if (s.Energy < 0.28 && s.WordsPerMinute < 105 && s.WordsPerMinute > 0)
            {
                moments.Add(new SpeakingMomentDto
                {
                    StartSeconds = s.StartSeconds,
                    EndSeconds = s.EndSeconds,
                    Issue = "LowConfidence",
                    Message = $"{timeLabel}: Low confidence detected — speak a bit louder and clearer."
                });
            }
        }

        // Hesitation: gap between speech segments (>1.2s pause after speech).
        for (var i = 0; i < segments.Count - 1; i++)
        {
            var gap = segments[i + 1].StartSeconds - segments[i].EndSeconds;
            if (gap >= 1.2 && segments[i].Energy > 0.2)
            {
                var at = segments[i].EndSeconds;
                moments.Add(new SpeakingMomentDto
                {
                    StartSeconds = at,
                    EndSeconds = at + gap,
                    Issue = "Hesitation",
                    Message = $"{FormatTime(at)}: Hesitation/pause detected — prepare your next phrase."
                });
            }
        }

        return moments
            .OrderBy(m => m.StartSeconds)
            .Take(8)
            .ToList();
    }

    public static string BuildSummary(List<SpeakingMomentDto> moments)
    {
        if (moments.Count == 0)
            return "Your speaking delivery sounds steady — no major nervous, harsh, or hesitant moments detected.";

        var issues = moments.Select(m => m.Issue).Distinct().ToList();
        return "Speaking coach: detected " + string.Join(", ", issues).ToLowerInvariant() +
               $" across {moments.Count} moment(s). See timeline below.";
    }

    private static string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return ts.TotalMinutes >= 1
            ? $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}"
            : $"{(int)ts.TotalSeconds}s";
    }
}

public class AudioSegmentDto
{
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public double Energy { get; set; }
    public double EnergyVariation { get; set; }
    public double PitchVariation { get; set; }
    public double WordsPerMinute { get; set; }
}
