using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Desktop.Models;

namespace PerceptionMapper.Desktop.Services;

public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public string? LastError { get; private set; }

    public ApiClient(string? baseUrl = null)
    {
        var url = (baseUrl ?? AppSettings.ApiBaseUrl).TrimEnd('/') + "/";
        _http = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(8) };
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new { username, password });
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions)
                : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<LoginResponse?> RegisterAsync(string username, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", new { username, password });
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions)
                : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AnalysisResponse?> AnalyzeAsync(
        string text,
        int userId,
        AudioProsodyMetricsDto? voiceMetrics = null,
        string? scenario = null,
        double durationSeconds = 0,
        int? rephraseTargetTone = null,
        int? declaredInputTone = null)
    {
        LastError = null;
        try
        {
            var body = new
            {
                text,
                userId,
                voiceMetrics,
                scenario,
                durationSeconds,
                rephraseTargetTone,
                declaredInputTone
            };
            var response = await _http.PostAsJsonAsync("api/analysis/analyze", body);
            if (!response.IsSuccessStatusCode)
            {
                var bodyText = await response.Content.ReadAsStringAsync();
                LastError = string.IsNullOrWhiteSpace(bodyText)
                    ? $"Server error ({(int)response.StatusCode} {response.ReasonPhrase})"
                    : bodyText;
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AnalysisResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public async Task<List<HistoryItem>?> GetHistoryAsync(int userId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<HistoryItem>>(
                $"api/analysis/history/{userId}", JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PracticeSessionItem>?> GetPracticeSessionsAsync(int userId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<PracticeSessionItem>>(
                $"api/analysis/practice-sessions/{userId}", JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
