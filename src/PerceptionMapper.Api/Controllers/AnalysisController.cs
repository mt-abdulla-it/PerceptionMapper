using Microsoft.AspNetCore.Mvc;
using PerceptionMapper.Api.Models;
using PerceptionMapper.Application.Interfaces;
using PerceptionMapper.Domain.Enums;

namespace PerceptionMapper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysis;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IPracticeSessionRepository _practiceRepository;

    public AnalysisController(
        IAnalysisService analysis,
        IAnalysisRepository analysisRepository,
        IPracticeSessionRepository practiceRepository)
    {
        _analysis = analysis;
        _analysisRepository = analysisRepository;
        _practiceRepository = practiceRepository;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
    {
        EmotionalTone? target = request.RephraseTargetTone is >= 0 and <= 4
            ? (EmotionalTone)request.RephraseTargetTone
            : null;

        EmotionalTone? declaredInput = request.DeclaredInputTone is >= 0 and <= 4
            ? (EmotionalTone)request.DeclaredInputTone.Value
            : null;

        var result = _analysis.Analyze(request.Text, request.VoiceMetrics, request.Scenario, target, declaredInput);

        if (request.UserId > 0 && !string.IsNullOrWhiteSpace(request.Text))
        {
            await _analysisRepository.SaveAnalysisAsync(request.UserId, request.Text, result);
            await _practiceRepository.SaveSessionAsync(
                request.UserId, result, request.Text, request.DurationSeconds);
        }

        return Ok(result);
    }

    [HttpGet("history/{userId:int}")]
    public async Task<IActionResult> History(int userId)
    {
        var records = await _analysisRepository.GetHistoryAsync(userId);
        var items = records.Select(r => new HistoryItemDto
        {
            Id = r.Id,
            InputText = r.InputText,
            DetectedLanguage = r.DetectedLanguage,
            DominantTone = r.DominantTone,
            FeedbackMessage = r.FeedbackMessage,
            AnalyzedAt = r.AnalyzedAt
        });
        return Ok(items);
    }

    [HttpGet("practice-sessions/{userId:int}")]
    public async Task<IActionResult> PracticeSessions(int userId)
    {
        var sessions = await _practiceRepository.GetSessionsAsync(userId);
        var items = sessions.Select(s => new PracticeSessionDto
        {
            Id = s.Id,
            Scenario = s.Scenario,
            DominantTone = s.DominantTone,
            CommunicationScore = s.CommunicationScore,
            DetectedLanguage = s.DetectedLanguage,
            DurationSeconds = s.DurationSeconds,
            MomentCount = s.MomentCount,
            InputPreview = s.InputPreview,
            CreatedAt = s.CreatedAt
        });
        return Ok(items);
    }
}
