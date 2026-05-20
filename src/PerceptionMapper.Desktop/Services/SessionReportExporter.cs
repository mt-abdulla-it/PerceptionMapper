using System.IO;
using System.Text;
using PerceptionMapper.Desktop.Models;

namespace PerceptionMapper.Desktop.Services;

public static class SessionReportExporter
{
    private static readonly string[] ToneNames =
        ["Neutral", "Assertive", "Empathetic", "Defensive", "Persuasive"];

    private static readonly string[] LangNames =
        ["Unknown", "English", "Tamil", "Sinhala"];

    public static string BuildReport(AnalysisResponse result, string inputText, string username)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PERCEPTION MAPPER — PRACTICE SESSION REPORT");
        sb.AppendLine($"User: {username}");
        sb.AppendLine($"Generated: {DateTime.Now:g}");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();
        sb.AppendLine("INPUT");
        sb.AppendLine(inputText);
        sb.AppendLine();
        sb.AppendLine($"Scenario: {result.Scenario}");
        sb.AppendLine($"Language: {LangNames.ElementAtOrDefault(result.Language) ?? result.Language.ToString()}");
        sb.AppendLine($"Communication score: {result.CommunicationScore}/100 — {result.ScoreLabel}");
        sb.AppendLine($"Dominant tone: {ToneNames.ElementAtOrDefault(result.DominantTone)} ({result.ToneConfidencePercent}%)");
        sb.AppendLine();
        sb.AppendLine("BIAS FLAGS");
        if (result.BiasFlags?.Count > 0)
        {
            foreach (var f in result.BiasFlags)
                sb.AppendLine($"  [{f.Severity}] {f.Category}: \"{f.Phrase}\" — {f.Explanation}");
        }
        else if (result.FlaggedBiasPhrases?.Count > 0)
        {
            foreach (var p in result.FlaggedBiasPhrases)
                sb.AppendLine($"  - {p}");
        }
        else
            sb.AppendLine("  None detected");

        if (result.FillerWords?.Count > 0)
            sb.AppendLine($"\nFiller words: {string.Join(", ", result.FillerWords)}");

        sb.AppendLine();
        sb.AppendLine("FEEDBACK");
        sb.AppendLine(result.FeedbackMessage);
        sb.AppendLine();
        sb.AppendLine("SUGGESTIONS");
        if (result.RephrasingSuggestions?.Count > 0)
            foreach (var s in result.RephrasingSuggestions)
                sb.AppendLine($"  • {s}");
        sb.AppendLine();
        sb.AppendLine("TARGET-TONE REPHRASES");
        if (result.TargetToneRephrases?.Count > 0)
            foreach (var s in result.TargetToneRephrases)
                sb.AppendLine($"  • {s}");

        if (result.SpeakingMoments?.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("SPEAKING MOMENTS");
            foreach (var m in result.SpeakingMoments)
                sb.AppendLine($"  [{m.StartSeconds:F1}s] {m.Message}");
        }

        return sb.ToString();
    }

    public static async Task<string> ExportAsync(AnalysisResponse result, string inputText, string username)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PerceptionMapper", "Reports");
        Directory.CreateDirectory(dir);

        var fileName = $"session_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var path = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(path, BuildReport(result, inputText, username));
        return path;
    }
}
