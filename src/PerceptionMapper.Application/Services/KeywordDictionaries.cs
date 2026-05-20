using PerceptionMapper.Application.Models;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

public static class KeywordDictionaries
{
    public static IReadOnlyDictionary<EmotionalTone, string[]> GetToneKeywords(InputLanguage language) =>
        language switch
        {
            InputLanguage.Tamil => TamilToneKeywords,
            InputLanguage.Sinhala => SinhalaToneKeywords,
            _ => EnglishToneKeywords
        };

    public static IReadOnlyList<BiasEntry> GetBiasEntries(InputLanguage language) =>
        language switch
        {
            InputLanguage.Tamil => TamilBiasEntries,
            InputLanguage.Sinhala => SinhalaBiasEntries,
            _ => EnglishBiasEntries
        };

    public static IReadOnlyList<string> GetBiasPhrases(InputLanguage language) =>
        GetBiasEntries(language).Select(e => e.Phrase).ToList();

    private static readonly Dictionary<EmotionalTone, string[]> EnglishToneKeywords = new()
    {
        [EmotionalTone.Assertive] =
        [
            "must", "have to", "need to", "will", "shall", "definitely", "certainly", "require", "demand",
            "insist", "non-negotiable", "expect", "mandatory", "clearly", "no choice", "i require",
            "strongly believe", "take responsibility", "act now", "full responsibility", "take charge",
            "move forward", "next steps", "clear plan", "definite"
        ],
        [EmotionalTone.Empathetic] =
        [
            "understand", "sorry", "apologize", "appreciate", "grateful", "thank you", "thanks",
            "feel your", "i hear you", "hear your concern", "concern", "support", "care about",
            "with you", "empathize", "truly sorry", "resolve this calmly"
        ],
        [EmotionalTone.Defensive] =
        [
            "not my fault", "you always", "you never", "however", "nevertheless", "but",
            "unfair", "blame", "your fault", "completely wrong", "excuse", "wasn't me",
            "did nothing wrong", "stop accusing"
        ],
        [EmotionalTone.Persuasive] =
        [
            "please consider", "i recommend", "recommend", "suggest", "benefit", "advantage",
            "opportunity", "imagine", "convince", "worth considering", "would you",
            "join us", "discover", "transform", "solution can"
        ],
        [EmotionalTone.Neutral] = ["information", "report", "data", "note", "according to", "summary", "stable numbers"]
    };

    private static readonly Dictionary<EmotionalTone, string[]> TamilToneKeywords = new()
    {
        [EmotionalTone.Assertive] =
        [
            "கட்டாயம்", "நிச்சயம்", "வேண்டும்", "தவிர", "செய்வேன்", "முடிவு", "பொறுப்பு", "உறுதியாக"
        ],
        [EmotionalTone.Empathetic] =
        [
            "மன்னிக்கவும்", "புரிகிறது", "நன்றி", "உதவி", "வருத்தம்", "உங்கள் உணர்வு", "ஆதரவு", "புரிந்துகொள்கிறேன்"
        ],
        [EmotionalTone.Defensive] =
        [
            "ஆனால்", "தவறு இல்லை", "நியாயமில்லை", "நீங்கள் எப்போதும்", "என் தவறு அல்ல", "குற்றம் சாட்ட"
        ],
        [EmotionalTone.Persuasive] =
        [
            "தயவுசெய்து", "பரிந்துரை", "நன்மை", "சிந்தியுங்கள்", "வாய்ப்பு", "பயன்"
        ],
        [EmotionalTone.Neutral] = ["தகவல்", "அறிக்கை", "இது", "தரவு", "சுருக்கம்"]
    };

    private static readonly Dictionary<EmotionalTone, string[]> SinhalaToneKeywords = new()
    {
        [EmotionalTone.Assertive] =
        [
            "අනිවාර්ය", "නිසැකවම", "අවශ්‍ය", "කළ යුතු", "කරනවා", "තීරණය", "වගකීම", "අනිවාර්යයි"
        ],
        [EmotionalTone.Empathetic] =
        [
            "සමාව", "අවබෝධයි", "ස්තූතියි", "උදව්", "කනගාටුයි", "ඔබේ හැඟීම", "සහාය", "තේරුම් ගන්නවා"
        ],
        [EmotionalTone.Defensive] =
        [
            "නමුත්", "දෝෂය නැත", "අයුතුයි", "ඔබ හැමවෙලේ", "මගේ වැරැද්ද නොවේ", "චෝදනා"
        ],
        [EmotionalTone.Persuasive] =
        [
            "කරුණාකර", "නිර්දේශ", "ප්‍රයෝජන", "සිතන්න", "අවස්ථාව", "ප්‍රතිලාභ"
        ],
        [EmotionalTone.Neutral] = ["තොරතුරු", "වාර්තාව", "මෙය", "දත්ත", "සාරාංශය"]
    };

    private static readonly List<BiasEntry> EnglishBiasEntries =
    [
        new("always lazy", "Age", "High", "Stereotype linking a group to negative traits."),
        new("those people", "RaceEthnicity", "High", "Othering language that generalizes groups."),
        new("you people", "RaceEthnicity", "High", "Divisive phrase that can signal prejudice."),
        new("boys don't cry", "Gender", "High", "Gender stereotype about emotional expression."),
        new("girls can't", "Gender", "High", "Limits ability based on gender."),
        new("typical woman", "Gender", "Medium", "Stereotype based on gender."),
        new("typical man", "Gender", "Medium", "Stereotype based on gender."),
        new("obviously stupid", "Ability", "High", "Insulting language about intelligence."),
        new("inferior race", "RaceEthnicity", "High", "Explicit racial hierarchy language."),
        new("old people are slow", "Age", "Medium", "Age-based generalization."),
        new("wheelchair bound", "Ability", "Medium", "Outdated disability phrasing — prefer person-first language.")
    ];

    private static readonly List<BiasEntry> TamilBiasEntries =
    [
        new("அவர்கள் எப்போதும்", "General", "Medium", "Overgeneralization about a group."),
        new("பெண்கள் முடியாது", "Gender", "High", "Gender-based limitation stereotype."),
        new("ஆண்கள் அழகூடாது", "Gender", "High", "Gender stereotype about emotions."),
        new("அந்த மக்கள்", "RaceEthnicity", "Medium", "Othering phrase."),
        new("கீழ்த்தரம்", "General", "High", "Derogatory class/status language."),
        new("பொதுவாக முட்டாள்", "Ability", "High", "Intelligence insult.")
    ];

    private static readonly List<BiasEntry> SinhalaBiasEntries =
    [
        new("ඔවුන් හැමවෙලේ", "General", "Medium", "Overgeneralization about a group."),
        new("ගැහැණු ළමයින් බැරි", "Gender", "High", "Gender-based limitation stereotype."),
        new("ඒ මිනිස්සු", "RaceEthnicity", "Medium", "Othering phrase."),
        new("පහළ cast", "RaceEthnicity", "High", "Caste-based hierarchy language."),
        new("සාමාන්‍යයෙන් මෝඩ", "Ability", "High", "Intelligence insult.")
    ];
}
