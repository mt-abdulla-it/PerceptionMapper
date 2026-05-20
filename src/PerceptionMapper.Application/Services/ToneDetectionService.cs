using System.Text.RegularExpressions;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

public class ToneDetectionService
{
    // When scores tie, prefer more specific interpersonal tones over generic ones.
    private static readonly EmotionalTone[] TieBreakOrder =
    [
        EmotionalTone.Empathetic,
        EmotionalTone.Defensive,
        EmotionalTone.Persuasive,
        EmotionalTone.Assertive,
        EmotionalTone.Neutral
    ];

    public Dictionary<EmotionalTone, int> DetectScores(string text, InputLanguage language)
    {
        var scores = Enum.GetValues<EmotionalTone>().ToDictionary(t => t, _ => 0);
        if (string.IsNullOrWhiteSpace(text))
            return scores;

        var normalized = text.Trim().ToLowerInvariant();
        var keywords = KeywordDictionaries.GetToneKeywords(language);

        // Longer phrases first so "not my fault" beats "not".
        var patterns = keywords
            .Where(kv => kv.Key != EmotionalTone.Neutral)
            .SelectMany(kv => kv.Value.Select(word => (Tone: kv.Key, Word: word.Trim().ToLowerInvariant())))
            .Where(p => p.Word.Length > 0)
            .OrderByDescending(p => p.Word.Length)
            .ToList();

        foreach (var (tone, word) in patterns)
        {
            var hits = CountMatches(normalized, word);
            if (hits <= 0) continue;

            // Multi-word cues and strong phrases count more (report: rule-based weighting).
            var weight = word.Contains(' ', StringComparison.Ordinal) ? 3 : 1;
            if (word.Length >= 8)
                weight = 2;

            scores[tone] += hits * weight;
        }

        var emotionalTotal = scores.Where(kv => kv.Key != EmotionalTone.Neutral).Sum(kv => kv.Value);
        if (emotionalTotal == 0)
            scores[EmotionalTone.Neutral] = 1;

        return scores;
    }

    public EmotionalTone GetDominantTone(Dictionary<EmotionalTone, int> scores)
    {
        var bestScore = scores.Max(kv => kv.Value);
        if (bestScore == 0)
            return EmotionalTone.Neutral;

        var leaders = scores
            .Where(kv => kv.Value == bestScore && kv.Key != EmotionalTone.Neutral)
            .Select(kv => kv.Key)
            .ToList();

        if (leaders.Count == 0)
            return EmotionalTone.Neutral;

        if (leaders.Count == 1)
            return leaders[0];

        return TieBreakOrder.First(t => leaders.Contains(t));
    }

    public bool IsAmbiguous(Dictionary<EmotionalTone, int> scores)
    {
        var ranked = scores
            .Where(kv => kv.Key != EmotionalTone.Neutral)
            .OrderByDescending(kv => kv.Value)
            .ToList();

        if (ranked.Count < 2 || ranked[0].Value == 0)
            return false;

        // Tie on top score, or second place within 1 point of leader.
        return ranked[0].Value == ranked[1].Value
               || (ranked[1].Value > 0 && ranked[0].Value - ranked[1].Value <= 1);
    }

    /// <summary>Combines tone from spoken voice (prosody) and transcribed words.</summary>
    public Dictionary<EmotionalTone, int> MergeScores(
        Dictionary<EmotionalTone, int> textScores,
        Dictionary<EmotionalTone, int> voiceScores,
        int voiceWeightPercent = 45)
    {
        voiceWeightPercent = Math.Clamp(voiceWeightPercent, 0, 100);
        var textWeight = 100 - voiceWeightPercent;

        var merged = Enum.GetValues<EmotionalTone>().ToDictionary(t => t, _ => 0);
        foreach (var tone in merged.Keys.ToList())
        {
            var text = textScores.GetValueOrDefault(tone, 0);
            var voice = voiceScores.GetValueOrDefault(tone, 0);
            merged[tone] = (text * textWeight + voice * voiceWeightPercent) / 100;
            if (merged[tone] == 0 && (text > 0 || voice > 0))
                merged[tone] = Math.Max(text, voice) > 0 ? 1 : 0;
        }

        return merged;
    }

    public int GetConfidencePercent(Dictionary<EmotionalTone, int> scores, EmotionalTone dominant)
    {
        var total = scores.Values.Sum();
        if (total == 0)
            return 0;

        var top = scores[dominant];
        return (int)Math.Round(100.0 * top / total);
    }

    private static int CountMatches(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return 0;

        // Phrases and long tokens: substring match.
        if (pattern.Contains(' ', StringComparison.Ordinal) || pattern.Length > 5)
            return CountOverlapping(text, pattern);

        // Short words: whole-word only (avoids "but" in "butter", "will" in "willing").
        var regex = new Regex($@"\b{Regex.Escape(pattern)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return regex.Matches(text).Count;
    }

    private static int CountOverlapping(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
