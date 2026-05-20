using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

/// <summary>
/// Estimates emotional tone from how the user spoke (energy, speed, pitch variation) — offline heuristics.
/// </summary>
public static class VoiceToneEstimator
{
    public static Dictionary<EmotionalTone, int> EstimateFromVoice(AudioProsodyMetricsDto m)
    {
        var scores = Enum.GetValues<EmotionalTone>().ToDictionary(t => t, _ => 0);

        // Normalized inputs (typical speech ranges).
        var energy = Clamp(m.AverageEnergy, 0, 1);
        var energyVar = Clamp(m.EnergyVariation, 0, 1);
        var wpm = m.WordsPerMinute;
        var pitchVar = Clamp(m.PitchVariation, 0, 1);

        // Assertive: louder + faster speech.
        if (energy > 0.45) scores[EmotionalTone.Assertive] += 2;
        if (wpm > 130) scores[EmotionalTone.Assertive] += 2;
        if (wpm > 160) scores[EmotionalTone.Assertive] += 1;

        // Empathetic: softer, slower, steadier.
        if (energy < 0.35) scores[EmotionalTone.Empathetic] += 2;
        if (wpm > 0 && wpm < 115) scores[EmotionalTone.Empathetic] += 2;
        if (pitchVar < 0.35) scores[EmotionalTone.Empathetic] += 1;

        // Defensive: stressed / uneven loudness.
        if (energyVar > 0.45) scores[EmotionalTone.Defensive] += 3;
        if (energy > 0.5 && energyVar > 0.35) scores[EmotionalTone.Defensive] += 1;

        // Persuasive: expressive pitch movement, moderate pace.
        if (pitchVar > 0.45) scores[EmotionalTone.Persuasive] += 2;
        if (wpm >= 110 && wpm <= 150) scores[EmotionalTone.Persuasive] += 1;

        // Neutral: calm, medium everything.
        if (energy >= 0.25 && energy <= 0.45 && energyVar < 0.35 && pitchVar < 0.4)
            scores[EmotionalTone.Neutral] += 2;

        if (scores.Where(kv => kv.Key != EmotionalTone.Neutral).All(kv => kv.Value == 0))
            scores[EmotionalTone.Neutral] = 1;

        return scores;
    }

    private static double Clamp(double v, double min, double max) =>
        v < min ? min : v > max ? max : v;
}
