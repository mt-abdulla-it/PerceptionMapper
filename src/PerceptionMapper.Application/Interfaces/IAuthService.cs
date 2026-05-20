using PerceptionMapper.Application.DTOs;

namespace PerceptionMapper.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResultDto> RegisterAsync(string username, string password);
    Task<LoginResultDto> LoginAsync(string username, string password);
}
