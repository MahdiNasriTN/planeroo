namespace Planeroo.Application.Interfaces;

/// <summary>
/// Provider-agnostic LLM client.
/// Switch between OpenAI / Anthropic / Gemini via appsettings "AI:Provider".
/// </summary>
public interface ILlmClient
{
    /// <summary>Name of the active provider, e.g. "OpenAI", "Anthropic", "Gemini".</summary>
    string ProviderName { get; }

    /// <summary>
    /// Text-only completion. Used for chat, study sheets, planning advice.
    /// </summary>
    Task<string?> CompleteAsync(
        string systemPrompt,
        string userMessage,
        int maxTokens = 1000,
        CancellationToken ct = default);

    /// <summary>
    /// Vision completion. Used for OCR / agenda scanning.
    /// </summary>
    Task<string?> VisionAsync(
        string systemPrompt,
        string userMessage,
        byte[] imageBytes,
        string mimeType,
        int maxTokens = 1500,
        CancellationToken ct = default);
}
