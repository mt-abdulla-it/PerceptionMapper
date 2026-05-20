using PerceptionMapper.Application.Services;
using PerceptionMapper.Domain.Enums;
using Xunit;

namespace PerceptionMapper.Tests;

public class ToneDetectionServiceTests
{
    private readonly ToneDetectionService _sut = new();

    [Theory]
    [InlineData("I strongly believe we must act now.", EmotionalTone.Assertive)]
    [InlineData("I am truly sorry and understand your concern.", EmotionalTone.Empathetic)]
    [InlineData("The report shows stable numbers this week.", EmotionalTone.Neutral)]
    [InlineData("This is your fault and completely wrong.", EmotionalTone.Defensive)]
    [InlineData("Join us and discover the benefits today.", EmotionalTone.Persuasive)]
    public void DetectScores_DominantTone_MatchesExpected(string text, EmotionalTone expected)
    {
        var scores = _sut.DetectScores(text, InputLanguage.English);
        var dominant = _sut.GetDominantTone(scores);
        Assert.Equal(expected, dominant);
    }

    [Fact]
    public void DetectScores_EmptyText_ReturnsNeutral()
    {
        var scores = _sut.DetectScores("", InputLanguage.English);
        Assert.Equal(EmotionalTone.Neutral, _sut.GetDominantTone(scores));
    }
}
