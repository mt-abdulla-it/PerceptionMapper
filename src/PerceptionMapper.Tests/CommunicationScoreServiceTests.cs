using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Enums;
using PerceptionMapper.Application.Services;
using PerceptionMapper.Domain.Enums;
using Xunit;

namespace PerceptionMapper.Tests;

public class CommunicationScoreServiceTests
{
    [Fact]
    public void Calculate_CleanInput_ReturnsHighScore()
    {
        var score = CommunicationScoreService.Calculate(
            EmotionalTone.Neutral,
            isAmbiguous: false,
            toneConfidence: 60,
            biasPhrases: [],
            moments: [],
            PracticeScenario.General);

        Assert.InRange(score, 85, 100);
    }

    [Fact]
    public void Calculate_BiasAndMoments_LowersScore()
    {
        var score = CommunicationScoreService.Calculate(
            EmotionalTone.Defensive,
            isAmbiguous: true,
            toneConfidence: 30,
            biasPhrases: ["those people", "always lazy"],
            moments:
            [
                new SpeakingMomentDto { Issue = "Harsh", Message = "test" },
                new SpeakingMomentDto { Issue = "Filler", Message = "um" }
            ],
            PracticeScenario.JobInterview);

        Assert.True(score < 70);
    }

    [Theory]
    [InlineData(90, "Excellent")]
    [InlineData(55, "Fair")]
    [InlineData(30, "Needs work")]
    public void ScoreLabel_ReturnsExpectedBand(int score, string expectedFragment)
    {
        var label = CommunicationScoreService.ScoreLabel(score);
        Assert.Contains(expectedFragment, label, StringComparison.OrdinalIgnoreCase);
    }
}
