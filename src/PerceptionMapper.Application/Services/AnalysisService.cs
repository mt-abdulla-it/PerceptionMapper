using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Enums;
using PerceptionMapper.Application.Interfaces;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

public class AnalysisService : IAnalysisService
{
    private readonly ToneDetectionService _tone = new();
    private readonly BiasDetectionService _bias = new();
    private readonly FeedbackService _feedback = new();

    public AnalysisResultDto Analyze(
        string text,
        AudioProsodyMetricsDto? voiceMetrics = null,
        string? scenarioId = null,
        EmotionalTone? rephraseTarget = null,
        EmotionalTone? declaredInputTone = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new AnalysisResultDto
            {
                FeedbackMessage = "Please enter text to analyze."
            };
        }

        var scenario = ScenarioCoachService.Parse(scenarioId);
        var language = LanguageDetector.Detect(text);
        var textScores = _tone.DetectScores(text, language);
        var toneScores = voiceMetrics is not null
            ? _tone.MergeScores(textScores, VoiceToneEstimator.EstimateFromVoice(voiceMetrics), voiceWeightPercent: 50)
            : textScores;
        var dominant = _tone.GetDominantTone(toneScores);
        var ambiguous = _tone.IsAmbiguous(toneScores);
        var confidence = _tone.GetConfidencePercent(toneScores, dominant);
        var biasFlags = _bias.DetectFlags(text, language);
        var flagged = biasFlags.Select(f => f.Phrase).ToList();
        var fillers = FillerWordService.DetectFillers(text);
        var (message, suggestions) = _feedback.Generate(dominant, ambiguous, flagged, language);
        var moments = voiceMetrics?.SpeakingMoments ?? new List<SpeakingMomentDto>();
        moments = moments.Concat(FillerWordService.BuildMoments(text)).ToList();

        var intentNote = BuildDeclaredInputVersusDetected(declaredInputTone, dominant);
        if (!string.IsNullOrEmpty(intentNote))
            message = intentNote + " " + message;

        if (fillers.Count > 0)
            suggestions.Add($"Reduce filler words ({string.Join(", ", fillers)}) — brief silence is better than 'um'.");

        if (moments.Count > 0)
            message = SpeakingCoachService.BuildSummary(moments) + " " + message;

        var scenarioTip = ScenarioCoachService.GetScenarioTip(scenario);
        var mismatch = ScenarioCoachService.GetToneMismatchAdvice(scenario, dominant);
        if (!string.IsNullOrEmpty(mismatch))
        {
            message += " " + mismatch;
            suggestions.Add(mismatch);
        }

        var score = CommunicationScoreService.Calculate(
            dominant, ambiguous, confidence, flagged, moments, scenario);

        var targetRephrases = rephraseTarget.HasValue
            ? ToneRephraseService.SuggestForTarget(text, rephraseTarget.Value)
            : ToneRephraseService.SuggestForTarget(text, SuggestTargetToneForScenario(scenario));

        return new AnalysisResultDto
        {
            Language = (int)language,
            DominantTone = (int)dominant,
            ToneScores = toneScores.ToDictionary(kv => (int)kv.Key, kv => kv.Value),
            IsToneAmbiguous = ambiguous,
            ToneConfidencePercent = confidence,
            UsedVoiceTone = voiceMetrics is not null,
            FlaggedBiasPhrases = flagged,
            BiasFlags = biasFlags,
            FillerWords = fillers,
            FeedbackMessage = message,
            RephrasingSuggestions = suggestions,
            SpeakingMoments = moments,
            Scenario = scenario.ToString(),
            CommunicationScore = score,
            ScoreLabel = CommunicationScoreService.ScoreLabel(score),
            ScenarioTip = scenarioTip,
            TargetToneRephrases = targetRephrases
        };
    }

    private static EmotionalTone SuggestTargetToneForScenario(PracticeScenario scenario) => scenario switch
    {
        PracticeScenario.Apology => EmotionalTone.Empathetic,
        PracticeScenario.JobInterview => EmotionalTone.Assertive,
        PracticeScenario.CustomerComplaint => EmotionalTone.Empathetic,
        PracticeScenario.TeamUpdate => EmotionalTone.Neutral,
        _ => EmotionalTone.Neutral
    };

    private static string? BuildDeclaredInputVersusDetected(EmotionalTone? declared, EmotionalTone detected)
    {
        if (!declared.HasValue)
            return null;

        var d = declared.Value;
        if (d == detected)
            return $"Your chosen input mood ({d}) matches how the message reads to the model.";

        return $"You labeled the input mood as {d}, but the text/voice reads closer to {detected}. Compare the Dominant tone panel below.";
    }
}
