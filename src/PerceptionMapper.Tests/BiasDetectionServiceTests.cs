using PerceptionMapper.Application.Services;
using PerceptionMapper.Domain.Enums;
using Xunit;

namespace PerceptionMapper.Tests;

public class BiasDetectionServiceTests
{
    private readonly BiasDetectionService _sut = new();

    [Fact]
    public void DetectFlags_BiasedSentence_ReturnsCategoryAndSeverity()
    {
        var flags = _sut.DetectFlags("Those people are always lazy.", InputLanguage.English);
        Assert.NotEmpty(flags);
        Assert.Contains(flags, f => !string.IsNullOrEmpty(f.Category));
        Assert.Contains(flags, f => !string.IsNullOrEmpty(f.Severity));
    }

    [Fact]
    public void DetectFlags_NeutralSentence_ReturnsEmpty()
    {
        var flags = _sut.DetectFlags("Thank you for your help today.", InputLanguage.English);
        Assert.Empty(flags);
    }

    [Fact]
    public void DetectFlaggedPhrases_GenderStereotype_MatchesPhrase()
    {
        var phrases = _sut.DetectFlaggedPhrases("Boys don't cry.", InputLanguage.English);
        Assert.Contains(phrases, p => p.Contains("boys", StringComparison.OrdinalIgnoreCase));
    }
}
