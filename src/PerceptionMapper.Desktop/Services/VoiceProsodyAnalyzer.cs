using System.IO;
using NAudio.Wave;
using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Services;
using PerceptionMapper.ML;

namespace PerceptionMapper.Desktop.Services;

/// <summary>Extracts speaking features and moment-by-moment coaching cues from a .wav recording.</summary>
public static class VoiceProsodyAnalyzer
{
    private const double SegmentSeconds = 2.5;
    private const double PauseThreshold = 0.04;

    public static AudioProsodyMetricsDto? AnalyzeFile(string? wavPath, string? recognizedText)
    {
        if (string.IsNullOrEmpty(wavPath) || !File.Exists(wavPath))
            return null;

        try
        {
            using var reader = new AudioFileReader(wavPath);
            var sampleRate = reader.WaveFormat.SampleRate;
            var buffer = new float[sampleRate / 4];
            var allSamples = new List<float>();

            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                allSamples.AddRange(buffer.Take(read));

            var duration = reader.TotalTime.TotalSeconds;
            if (duration < 0.4 || allSamples.Count == 0)
                return null;

            var segments = BuildSegments(allSamples, sampleRate, recognizedText);
            using var hybrid = new HybridEmotionAnalyzer();
            var mlWindows = hybrid.IsMlEnabled
                ? new List<(double, float[])> { (0, allSamples.Take(Math.Min(allSamples.Count, sampleRate * 3)).ToArray()) }
                : null;
            var moments = hybrid.AnalyzeSegments(segments, mlWindows);

            var energies = segments.Select(s => s.Energy).ToList();
            var avgEnergy = energies.Count > 0 ? energies.Average() : 0;
            var energyVar = energies.Count > 1 ? StdDev(energies) : 0;
            var pitchVars = segments.Select(s => s.PitchVariation).Where(v => v > 0).ToList();
            var avgPitchVar = pitchVars.Count > 0 ? pitchVars.Average() : 0;

            var wordCount = string.IsNullOrWhiteSpace(recognizedText)
                ? 0
                : recognizedText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            var wpm = wordCount > 0 ? wordCount / (duration / 60.0) : 120;

            return new AudioProsodyMetricsDto
            {
                AverageEnergy = Normalize(avgEnergy, 0.02, 0.25),
                EnergyVariation = Normalize(energyVar, 0.005, 0.08),
                WordsPerMinute = wpm,
                PitchVariation = Normalize(avgPitchVar, 0.05, 0.45),
                DurationSeconds = duration,
                SpeakingMoments = moments
            };
        }
        catch
        {
            return null;
        }
    }

    private static List<AudioSegmentDto> BuildSegments(List<float> samples, int sampleRate, string? text)
    {
        var segmentSamples = (int)(SegmentSeconds * sampleRate);
        var segments = new List<AudioSegmentDto>();
        var totalWords = string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var totalDuration = samples.Count / (double)sampleRate;
        var wordsPerSec = totalDuration > 0 && totalWords > 0 ? totalWords / totalDuration : 2.0;

        for (var offset = 0; offset < samples.Count; offset += segmentSamples)
        {
            var chunk = samples.Skip(offset).Take(segmentSamples).ToArray();
            if (chunk.Length < sampleRate / 10)
                continue;

            var rms = Math.Sqrt(chunk.Select(s => s * s).Average());
            if (rms < PauseThreshold)
                continue;

            var start = offset / (double)sampleRate;
            var end = (offset + chunk.Length) / (double)sampleRate;
            var windowEnergies = WindowRms(chunk, sampleRate / 8);
            var pitchSamples = new List<double>();

            for (var i = 0; i < chunk.Length; i += sampleRate / 8)
            {
                var len = Math.Min(sampleRate / 8, chunk.Length - i);
                var p = EstimatePitchHz(chunk, i, len, sampleRate);
                if (p > 50 && p < 500) pitchSamples.Add(p);
            }

            var pitchVar = pitchSamples.Count > 2 && pitchSamples.Average() > 0
                ? StdDev(pitchSamples) / pitchSamples.Average()
                : 0;

            var segDuration = end - start;
            var estWords = Math.Max(1, (int)Math.Round(wordsPerSec * segDuration));

            segments.Add(new AudioSegmentDto
            {
                StartSeconds = start,
                EndSeconds = end,
                Energy = Normalize(rms, 0.02, 0.25),
                EnergyVariation = Normalize(windowEnergies.Count > 1 ? StdDev(windowEnergies) : 0, 0.005, 0.08),
                PitchVariation = Normalize(pitchVar, 0.05, 0.45),
                WordsPerMinute = segDuration > 0 ? estWords / (segDuration / 60.0) : 0
            });
        }

        return segments;
    }

    private static List<double> WindowRms(float[] chunk, int windowSize)
    {
        var list = new List<double>();
        for (var i = 0; i < chunk.Length; i += windowSize)
        {
            var len = Math.Min(windowSize, chunk.Length - i);
            var rms = Math.Sqrt(chunk.Skip(i).Take(len).Select(s => s * s).Average());
            list.Add(rms);
        }

        return list;
    }

    private static double EstimatePitchHz(float[] buffer, int offset, int count, int sampleRate)
    {
        if (count < sampleRate / 50) return 0;
        var minLag = sampleRate / 500;
        var maxLag = Math.Min(sampleRate / 50, count - 1);
        var bestCorr = 0.0;
        var bestLag = 0;

        for (var lag = minLag; lag < maxLag; lag++)
        {
            double corr = 0;
            for (var i = 0; i < count - lag; i++)
                corr += buffer[offset + i] * buffer[offset + i + lag];

            if (corr > bestCorr)
            {
                bestCorr = corr;
                bestLag = lag;
            }
        }

        return bestLag > 0 ? (double)sampleRate / bestLag : 0;
    }

    private static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count < 2) return 0;
        var avg = values.Average();
        return Math.Sqrt(values.Select(v => (v - avg) * (v - avg)).Average());
    }

    private static double Normalize(double value, double min, double max)
    {
        if (max <= min) return 0;
        return Math.Clamp((value - min) / (max - min), 0, 1);
    }
}
