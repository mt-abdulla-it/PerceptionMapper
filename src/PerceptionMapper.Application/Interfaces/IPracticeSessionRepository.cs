using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Domain.Entities;

namespace PerceptionMapper.Application.Interfaces;

public interface IPracticeSessionRepository
{
    Task SaveSessionAsync(int userId, AnalysisResultDto result, string inputText, double durationSeconds);
    Task<IReadOnlyList<PracticeSession>> GetSessionsAsync(int userId, int take = 30);
}
