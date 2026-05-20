using System.IO;
using System.Text.Json;

namespace PerceptionMapper.Desktop.Services;

public static class AppSettings
{
    private const string DefaultApiUrl = "http://localhost:5050";

    public static string ApiBaseUrl { get; } = LoadApiBaseUrl();

    private static string LoadApiBaseUrl()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return DefaultApiUrl;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("Api", out var api) &&
                api.TryGetProperty("BaseUrl", out var url))
            {
                var value = url.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.TrimEnd('/');
            }
        }
        catch
        {
            // use default
        }

        return DefaultApiUrl;
    }
}
