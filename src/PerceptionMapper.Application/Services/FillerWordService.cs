using System.Text.RegularExpressions;
using PerceptionMapper.Application.DTOs;

namespace PerceptionMapper.Application.Services;

public static class FillerWordService
{
    private static readonly string[] Fillers =
    [
        "um", "uh", "er", "ah", "like", "you know", "basically", "actually", "sort of", "kind of"
    ];

    public static List<string> DetectFillers(string text)
    {
        var found = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return found;

        var lower = text.ToLowerInvariant();
        foreach (var filler in Fillers.OrderByDescending(f => f.Length))
        {
            var pattern = filler.Contains(' ', StringComparison.Ordinal)
                ? Regex.Escape(filler)
                : $@"\b{Regex.Escape(filler)}\b";

            if (Regex.IsMatch(lower, pattern, RegexOptions.IgnoreCase))
                found.Add(filler);
        }

        return found.Distinct().ToList();
    }

    public static List<SpeakingMomentDto> BuildMoments(string text)
    {
        var moments = new List<SpeakingMomentDto>();
        var fillers = DetectFillers(text);
        if (fillers.Count == 0)
            return moments;

        moments.Add(new SpeakingMomentDto
        {
            StartSeconds = 0,
            EndSeconds = 0,
            Issue = "Filler",
            Message = $"Filler words detected ({string.Join(", ", fillers)}) — pause instead of filler sounds."
        });

        return moments;
    }
}
