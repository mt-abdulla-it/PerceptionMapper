using System.Diagnostics;
using System.Text.Json;
using PerceptionMapper.Application.Services;

namespace PerceptionMapper.EvalTool;

internal static class Program
{
    private static readonly string[] ToneNames =
        ["Neutral", "Assertive", "Empathetic", "Defensive", "Persuasive"];

    public static int Main(string[] args)
    {
        var strict = args.Contains("--strict", StringComparer.OrdinalIgnoreCase);
        var toneOnly = args.Contains("--tone", StringComparer.OrdinalIgnoreCase);
        var biasOnly = args.Contains("--bias", StringComparer.OrdinalIgnoreCase);
        var runTone = !biasOnly;
        var runBias = !toneOnly;

        var failures = 0;
        if (runTone)
            failures += RunToneEval(strict);
        if (runBias)
            failures += RunBiasEval(strict);

        if (Debugger.IsAttached)
        {
            Console.WriteLine();
            Console.WriteLine("Press Enter to close...");
            Console.ReadLine();
        }

        return strict && failures > 0 ? 2 : 0;
    }

    private static int RunToneEval(bool strict)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "tone_eval_samples.json");
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "tone_eval_samples.json"));

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Tone samples not found: {path}");
            return strict ? 1 : 0;
        }

        var samples = JsonSerializer.Deserialize<List<ToneSample>>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var analyzer = new AnalysisService();
        var correct = 0;
        var failCount = 0;

        Console.WriteLine("=== Tone accuracy ===");
        Console.WriteLine(new string('-', 72));

        foreach (var sample in samples)
        {
            var result = analyzer.Analyze(sample.Text, scenarioId: sample.Scenario);
            var predicted = ToneNames.ElementAtOrDefault(result.DominantTone) ?? "?";
            var ok = string.Equals(predicted, sample.ExpectedTone, StringComparison.OrdinalIgnoreCase);
            if (ok) correct++;
            else failCount++;

            Console.WriteLine(ok ? "[PASS]" : "[FAIL]");
            Console.WriteLine($"  {Truncate(sample.Text, 60)}");
            Console.WriteLine($"  Expected: {sample.ExpectedTone} | Got: {predicted} ({result.ToneConfidencePercent}%)");
        }

        var pct = samples.Count > 0 ? correct * 100.0 / samples.Count : 0;
        Console.WriteLine($"Tone accuracy: {correct}/{samples.Count} ({pct:F1}%)\n");
        return failCount;
    }

    private static int RunBiasEval(bool strict)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "bias_eval_samples.json");
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "bias_eval_samples.json"));

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Bias samples not found: {path}");
            return strict ? 1 : 0;
        }

        var samples = JsonSerializer.Deserialize<List<BiasSample>>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var bias = new BiasDetectionService();
        var failCount = 0;
        var correct = 0;

        Console.WriteLine("=== Bias detection ===");
        Console.WriteLine(new string('-', 72));

        foreach (var sample in samples)
        {
            var flags = bias.DetectFlags(sample.Text, Domain.Enums.InputLanguage.English);
            var hasBias = flags.Count >= sample.MinFlags;
            var ok = sample.ExpectBias ? hasBias : flags.Count == 0;
            if (ok) correct++;
            else failCount++;

            Console.WriteLine(ok ? "[PASS]" : "[FAIL]");
            Console.WriteLine($"  {Truncate(sample.Text, 60)}");
            Console.WriteLine($"  Expect bias: {sample.ExpectBias} | Flags: {flags.Count} ({string.Join(", ", flags.Select(f => f.Phrase))})");
        }

        var pct = samples.Count > 0 ? correct * 100.0 / samples.Count : 0;
        Console.WriteLine($"Bias accuracy: {correct}/{samples.Count} ({pct:F1}%)\n");
        return failCount;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";

    private sealed class ToneSample
    {
        public string Text { get; set; } = "";
        public string ExpectedTone { get; set; } = "";
        public string Scenario { get; set; } = "General";
    }

    private sealed class BiasSample
    {
        public string Text { get; set; } = "";
        public bool ExpectBias { get; set; }
        public int MinFlags { get; set; }
    }
}
