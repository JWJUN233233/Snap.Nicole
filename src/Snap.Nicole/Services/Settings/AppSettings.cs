namespace Snap.Nicole.Services.Settings;

internal sealed class AppSettings
{
    public string Language { get; set; } = "zh-CN";

    public string? OpenAIApiKey { get; set; }

    public string? OpenAIBaseUrl { get; set; }

    public string DefaultModel { get; set; } = "gpt-4o";
}
