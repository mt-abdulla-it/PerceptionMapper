using System.Globalization;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace PerceptionMapper.Desktop.Services;

public static class MultilingualVoiceHelper
{
    public static string GetInstalledSummary()
    {
        var rec = SpeechRecognitionEngine.InstalledRecognizers()
            .Select(r => r.Culture.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var tts = new SpeechSynthesizer().GetInstalledVoices()
            .Select(v => v.VoiceInfo.Culture.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return "STT: " + (rec.Count == 0 ? "none" : string.Join(", ", rec)) +
               "\nTTS: " + (tts.Count == 0 ? "none" : string.Join(", ", tts));
    }

    public static bool IsRecognizerReady(string cultureName) =>
        SpeechRecognitionEngine.InstalledRecognizers()
            .Any(r => r.Culture.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase)
                      || r.Culture.TwoLetterISOLanguageName == CultureInfo.GetCultureInfo(cultureName).TwoLetterISOLanguageName);

    public static bool IsTtsReady(string cultureName) =>
        new SpeechSynthesizer().GetInstalledVoices()
            .Any(v => v.VoiceInfo.Culture.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase));

    public static void OpenLanguageSettings() =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:language") { UseShellExecute = true });
}
