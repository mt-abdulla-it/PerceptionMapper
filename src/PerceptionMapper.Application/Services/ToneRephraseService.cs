using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

/// <summary>Suggests rewrites toward a target tone (rule-based, offline).</summary>
public static class ToneRephraseService
{
    public static List<string> SuggestForTarget(string text, EmotionalTone targetTone)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return targetTone switch
        {
            EmotionalTone.Empathetic =>
            [
                "I understand how this affects you, and I want to help.",
                "Thank you for sharing — I hear your concern.",
                text.Replace("must", "could").Replace("You need to", "Perhaps we can")
            ],
            EmotionalTone.Assertive =>
            [
                "Please complete this by Friday — it is important for the team.",
                "I recommend we move forward with this plan.",
                text + " Let's align on next steps."
            ],
            EmotionalTone.Neutral =>
            [
                "Here is the update: " + text,
                "The information below summarizes the situation.",
                text.Replace("!", ".")
            ],
            EmotionalTone.Persuasive =>
            [
                "Please consider this option — it offers clear benefits.",
                "I suggest we try this approach because it reduces risk.",
                text
            ],
            EmotionalTone.Defensive =>
            [
                "I see your point. Here are the facts as I understand them.",
                "Let me clarify my perspective without assigning blame.",
                text.Replace("not my fault", "here is what happened")
            ],
            _ => new List<string> { text }
        };
    }
}
