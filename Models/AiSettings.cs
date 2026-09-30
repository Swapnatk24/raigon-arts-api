namespace RaigonArts.Api.Models;

public class AiSettings
{
    public const string SectionName = "AiAssistant";

    /// <summary>
    /// AI provider type: "Gemini", "OpenAI", "AzureOpenAI", "None"
    /// </summary>
    public string Provider { get; set; } = "Gemini";

    /// <summary>
    /// API Key for OpenAI provider. Should NOT be hardcoded in appsettings.json.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API Key for Google Gemini provider. Loaded from User Secrets or environment variable.
    /// </summary>
    public string GeminiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API Key for Groq provider. Loaded from User Secrets or environment variable.
    /// </summary>
    public string GroqApiKey { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI Model name (e.g., "gpt-4o-mini", "gpt-4o")
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Google Gemini Model name (e.g., "gemini-1.5-flash", "gemini-1.5-pro", "gemini-2.0-flash")
    /// </summary>
    public string GeminiModel { get; set; } = "gemini-1.5-flash";

    /// <summary>
    /// Groq Model name (e.g., "llama-3.3-70b-versatile", "llama-3.1-70b-versatile", "llama-3.1-8b-instant")
    /// </summary>
    public string GroqModel { get; set; } = "llama-3.3-70b-versatile";

    /// <summary>
    /// Optional custom base URL or endpoint (e.g. for Azure OpenAI or local Ollama)
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Temperature for LLM completions. Default 0.2 for high precision and tool adherence.
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Max tokens for LLM completions.
    /// </summary>
    public int MaxTokens { get; set; } = 1500;

    /// <summary>
    /// Inactive conversation session expiry in minutes. Default 120 (2 hours).
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 120;

    /// <summary>
    /// Default country calling code for phone number normalization (e.g. "91" for India).
    /// </summary>
    public string DefaultCountryCode { get; set; } = "91";
}

