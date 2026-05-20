using Microsoft.EntityFrameworkCore;
using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Interfaces;
using PerceptionMapper.Domain.Entities;
using PerceptionMapper.Infrastructure.Data;
using PerceptionMapper.Infrastructure.Security;

namespace PerceptionMapper.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    public async Task<LoginResultDto> RegisterAsync(string username, string password)
    {
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Fail("Username and password are required.");

        if (await _db.Users.AnyAsync(u => u.Username == username))
            return Fail("Username already exists.");

        var (hash, salt) = PasswordHasher.HashPassword(password);
        var user = new User { Username = username, PasswordHash = hash, Salt = salt };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Success(user);
    }

    public async Task<LoginResultDto> LoginAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.Trim());
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash, user.Salt))
            return Fail("Invalid username or password.");

        return Success(user);
    }

    private static LoginResultDto Success(User user) => new()
    {
        Success = true,
        Message = "Login successful.",
        UserId = user.Id,
        Username = user.Username,
        Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
    };

    private static LoginResultDto Fail(string message) => new() { Success = false, Message = message };
}
