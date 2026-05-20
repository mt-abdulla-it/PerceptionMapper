using Microsoft.Extensions.DependencyInjection;
using PerceptionMapper.Application.Interfaces;
using PerceptionMapper.Application.Services;

namespace PerceptionMapper.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IAnalysisService, AnalysisService>();
        return services;
    }
}
