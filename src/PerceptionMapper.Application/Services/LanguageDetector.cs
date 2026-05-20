using System.Text.RegularExpressions;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

public static class LanguageDetector
{
    private static readonly Regex TamilRegex = new(@"[\u0B80-\u0BFF]", RegexOptions.Compiled);
    private static readonly Regex SinhalaRegex = new(@"[\u0D80-\u0DFF]", RegexOptions.Compiled);

    public static InputLanguage Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return InputLanguage.Unknown;

        var tamilCount = TamilRegex.Matches(text).Count;
        var sinhalaCount = SinhalaRegex.Matches(text).Count;

        if (tamilCount > sinhalaCount && tamilCount > 0)
            return InputLanguage.Tamil;
        if (sinhalaCount > tamilCount && sinhalaCount > 0)
            return InputLanguage.Sinhala;

        return InputLanguage.English;
    }
}
