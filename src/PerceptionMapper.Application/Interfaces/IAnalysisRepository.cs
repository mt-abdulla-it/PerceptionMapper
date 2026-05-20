using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Domain.Entities;

namespace PerceptionMapper.Application.Interfaces;

public interface IAnalysisRepository
{
    Task SaveAnalysisAsync(int userId, string inputText, AnalysisResultDto result);
    Task<IReadOnlyList<AnalysisRecord>> GetHistoryAsync(int userId, int take = 20);
}
