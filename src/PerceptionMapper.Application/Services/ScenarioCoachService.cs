using PerceptionMapper.Application.Enums;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Application.Services;

public static class ScenarioCoachService
{
    public static string GetDisplayName(PracticeScenario scenario) => scenario switch
    {
        PracticeScenario.JobInterview => "Job interview",
        PracticeScenario.Apology => "Apology",
        PracticeScenario.TeamUpdate => "Team update",
        PracticeScenario.CustomerComplaint => "Customer complaint",
        _ => "General practice"
    };

    public static PracticeScenario Parse(string? id) =>
        Enum.TryParse<PracticeScenario>(id, true, out var s) ? s : PracticeScenario.General;

    public static string GetScenarioTip(PracticeScenario scenario) => scenario switch
    {
        PracticeScenario.JobInterview =>
            "Interview mode: stay confident, clear, and professional. Avoid defensive or harsh phrasing.",
        PracticeScenario.Apology =>
            "Apology mode: use empathetic, calm tone. Acknowledge impact without blaming others.",
        PracticeScenario.TeamUpdate =>
            "Team update mode: be neutral-to-assertive, concise, and respectful.",
        PracticeScenario.CustomerComplaint =>
            "Customer complaint mode: stay calm, empathetic, and solution-focused.",
        _ => "General practice: aim for clear, respectful communication."
    };

    public static bool IsToneFitForScenario(PracticeScenario scenario, EmotionalTone tone) => scenario switch
    {
        PracticeScenario.JobInterview => tone is EmotionalTone.Assertive or EmotionalTone.Persuasive or EmotionalTone.Neutral,
        PracticeScenario.Apology => tone is EmotionalTone.Empathetic or EmotionalTone.Neutral,
        PracticeScenario.TeamUpdate => tone is EmotionalTone.Neutral or EmotionalTone.Assertive or EmotionalTone.Persuasive,
        PracticeScenario.CustomerComplaint => tone is EmotionalTone.Empathetic or EmotionalTone.Neutral,
        _ => true
    };

    public static string GetToneMismatchAdvice(PracticeScenario scenario, EmotionalTone tone)
    {
        if (IsToneFitForScenario(scenario, tone))
            return "";

        return scenario switch
        {
            PracticeScenario.JobInterview => "For interviews, try a more confident and professional tone.",
            PracticeScenario.Apology => "For apologies, soften your tone and show empathy.",
            PracticeScenario.TeamUpdate => "For team updates, keep tone clear and neutral.",
            PracticeScenario.CustomerComplaint => "For customer issues, use calm and empathetic language.",
            _ => ""
        };
    }
}
