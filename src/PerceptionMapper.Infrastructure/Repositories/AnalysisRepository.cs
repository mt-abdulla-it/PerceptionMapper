using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Interfaces;
using PerceptionMapper.Domain.Entities;
using PerceptionMapper.Infrastructure.Data;

namespace PerceptionMapper.Infrastructure.Repositories;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly AppDbContext _db;

    public AnalysisRepository(AppDbContext db) => _db = db;

    public async Task SaveAnalysisAsync(int userId, string inputText, AnalysisResultDto result)
    {
        var record = new AnalysisRecord
        {
            UserId = userId,
            InputText = inputText,
            DetectedLanguage = ((PerceptionMapper.Domain.Enums.InputLanguage)result.Language).ToString(),
            DominantTone = ((PerceptionMapper.Domain.Enums.EmotionalTone)result.DominantTone).ToString(),
            ToneScoresJson = JsonSerializer.Serialize(result.ToneScores),
            FlaggedBiasJson = JsonSerializer.Serialize(result.FlaggedBiasPhrases),
            FeedbackMessage = result.FeedbackMessage,
            RephrasingSuggestions = string.Join("\n", result.RephrasingSuggestions)
        };

        _db.AnalysisRecords.Add(record);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AnalysisRecord>> GetHistoryAsync(int userId, int take = 20) =>
        await _db.AnalysisRecords
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AnalyzedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
}
