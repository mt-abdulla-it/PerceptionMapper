using PerceptionMapper.Application.Services;
using Xunit;

namespace PerceptionMapper.Tests;

public class FillerWordServiceTests
{
    [Fact]
    public void DetectFillers_WithUmAndLike_ReturnsBoth()
    {
        var fillers = FillerWordService.DetectFillers("So um, I think, like, we should start.");
        Assert.Contains("um", fillers);
        Assert.Contains("like", fillers);
    }

    [Fact]
    public void DetectFillers_CleanSentence_ReturnsEmpty()
    {
        var fillers = FillerWordService.DetectFillers("We will begin the meeting at nine.");
        Assert.Empty(fillers);
    }
}
