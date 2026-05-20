using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

public class BiasDetectionService
{
    public List<BiasFlagDto> DetectFlags(string text, InputLanguage language)
    {
        var flags = new List<BiasFlagDto>();
        if (string.IsNullOrWhiteSpace(text))
            return flags;

        var lower = text.ToLowerInvariant();
        foreach (var entry in KeywordDictionaries.GetBiasEntries(language)
                     .OrderByDescending(e => e.Phrase.Length))
        {
            if (!lower.Contains(entry.Phrase.ToLowerInvariant(), StringComparison.Ordinal))
                continue;

            flags.Add(new BiasFlagDto
            {
                Phrase = entry.Phrase,
                Category = entry.Category,
                Severity = entry.Severity,
                Explanation = string.IsNullOrWhiteSpace(entry.Explanation)
                    ? GetDefaultExplanation(entry.Category, entry.Severity)
                    : entry.Explanation
            });
        }

        return flags
            .GroupBy(f => f.Phrase, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public List<string> DetectFlaggedPhrases(string text, InputLanguage language) =>
        DetectFlags(text, language).Select(f => f.Phrase).ToList();

    private static string GetDefaultExplanation(string category, string severity) =>
        $"Flagged ({severity}): possible {category.ToLowerInvariant()} bias — use inclusive, person-first wording.";
}
