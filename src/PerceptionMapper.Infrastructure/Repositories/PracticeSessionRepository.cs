using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Interfaces;
using PerceptionMapper.Domain.Entities;
using PerceptionMapper.Infrastructure.Data;

namespace PerceptionMapper.Infrastructure.Repositories;

public class PracticeSessionRepository : IPracticeSessionRepository
{
    private readonly AppDbContext _db;

    public PracticeSessionRepository(AppDbContext db) => _db = db;

    public async Task SaveSessionAsync(int userId, AnalysisResultDto result, string inputText, double durationSeconds)
    {
        var preview = inputText.Length <= 120 ? inputText : inputText[..120] + "...";
        var session = new PracticeSession
        {
            UserId = userId,
            Scenario = result.Scenario,
            DominantTone = ((Domain.Enums.EmotionalTone)result.DominantTone).ToString(),
            CommunicationScore = result.CommunicationScore,
            DetectedLanguage = ((Domain.Enums.InputLanguage)result.Language).ToString(),
            DurationSeconds = durationSeconds,
            MomentCount = result.SpeakingMoments.Count,
            InputPreview = preview,
            MomentsJson = JsonSerializer.Serialize(result.SpeakingMoments)
        };

        _db.PracticeSessions.Add(session);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PracticeSession>> GetSessionsAsync(int userId, int take = 30) =>
        await _db.PracticeSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
}
