namespace Dymatrix.Notifications.Infrastructure.Settings;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string? ApiKey { get; init; }

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

    public string Model { get; init; } = "gemini-3.6-flash";
}
