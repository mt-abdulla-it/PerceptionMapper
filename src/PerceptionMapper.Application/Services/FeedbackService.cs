using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

public class FeedbackService
{
    public (string Message, List<string> Suggestions) Generate(
        EmotionalTone dominantTone,
        bool isAmbiguous,
        List<string> flaggedBias,
        InputLanguage language)
    {
        var parts = new List<string>();
        var suggestions = new List<string>();

        if (isAmbiguous)
        {
            parts.Add("Tone may be unclear or misaligned. Consider simplifying your message.");
            suggestions.Add(GetToneSuggestion(dominantTone, language));
        }
        else
        {
            parts.Add($"Dominant tone detected: {dominantTone}.");
        }

        if (flaggedBias.Count > 0)
        {
            parts.Add("Bias indicators detected. Use more inclusive language.");
            foreach (var phrase in flaggedBias)
                suggestions.Add(GetInclusiveAlternative(phrase, language));
        }
        else if (flaggedBias.Count == 0 && !isAmbiguous)
        {
            parts.Add("No significant bias detected.");
        }

        return (string.Join(" ", parts), suggestions);
    }

    private static string GetToneSuggestion(EmotionalTone tone, InputLanguage language) => (language, tone) switch
    {
        (InputLanguage.Tamil, EmotionalTone.Assertive) => "மென்மையான சொற்களைப் பயன்படுத்தி மீண்டும் எழுதுங்கள்.",
        (InputLanguage.Sinhala, EmotionalTone.Assertive) => "මෘදු වචන භාවිතා කර නැවත ලියන්න.",
        (_, EmotionalTone.Defensive) => "Rephrase using 'I' statements and focus on facts, not blame.",
        (_, EmotionalTone.Assertive) => "Soften directives: try 'Could we...' instead of 'You must...'.",
        (_, EmotionalTone.Empathetic) => "Your tone is warm. Keep acknowledging the other person's view.",
        _ => "Consider a clearer opening sentence that states your intent."
    };

    private static string GetInclusiveAlternative(string phrase, InputLanguage language) => language switch
    {
        InputLanguage.Tamil => $"'{phrase}' — சமானமான, மரியாதையான சொற்களைத் தேர்ந்தெடுக்கவும்.",
        InputLanguage.Sinhala => $"'{phrase}' — සමාන, ගෞරවනීය වචන භාවිතා කරන්න.",
        _ => $"Replace '{phrase}' with neutral, person-first wording."
    };
}
