using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Enums;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

public static class CommunicationScoreService
{
    public static int Calculate(
        EmotionalTone dominantTone,
        bool isAmbiguous,
        int toneConfidence,
        IReadOnlyList<string> biasPhrases,
        IReadOnlyList<SpeakingMomentDto> moments,
        PracticeScenario scenario)
    {
        var score = 100;

        score -= biasPhrases.Count * 12;
        score -= moments.Count(m => m.Issue is "Harsh" or "Nervous") * 8;
        score -= moments.Count(m => m.Issue is "Hesitation") * 5;
        score -= moments.Count(m => m.Issue is "Filler") * 4;
        score -= moments.Count(m => m.Issue is "LowConfidence") * 6;

        if (isAmbiguous)
            score -= 10;

        if (toneConfidence > 0 && toneConfidence < 40)
            score -= 8;

        if (!ScenarioCoachService.IsToneFitForScenario(scenario, dominantTone))
            score -= 15;

        return Math.Clamp(score, 0, 100);
    }

    public static string ScoreLabel(int score) => score switch
    {
        >= 85 => "Excellent — professional delivery",
        >= 70 => "Good — minor improvements possible",
        >= 50 => "Fair — practice recommended",
        _ => "Needs work — review coach feedback"
    };
}
