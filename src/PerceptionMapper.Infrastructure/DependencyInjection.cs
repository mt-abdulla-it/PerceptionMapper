using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerceptionMapper.Application.Interfaces;
using PerceptionMapper.Infrastructure.Data;
using PerceptionMapper.Infrastructure.Repositories;
using PerceptionMapper.Infrastructure.Services;

namespace PerceptionMapper.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        services.AddScoped<IPracticeSessionRepository, PracticeSessionRepository>();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DatabaseSchemaPatcher.ApplyAsync(db);

        if (!await db.Users.AnyAsync())
        {
            var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
            await auth.RegisterAsync("demo", "Demo@123");
        }
    }
}
