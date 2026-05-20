namespace PerceptionMapper.ML;

public sealed class EmotionPrediction
{
    public string TopEmotion { get; init; } = "neutral";
    public float TopScore { get; init; }
    public IReadOnlyDictionary<string, float> Scores { get; init; } =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
}
