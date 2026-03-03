using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Planeroo.Application.Interfaces;

namespace Planeroo.Infrastructure.Services.LlmProviders;

/// <summary>
/// Google Gemini provider — uses the generateContent REST API.
/// Supports gemini-1.5-pro / gemini-2.0-flash and multimodal vision.
/// </summary>
public class GeminiLlmClient : ILlmClient
{
    private readonly IHttpClientFactory _http;
    private readonly string _apiKey;
    private readonly string _ocrModel;
    private readonly string _chatModel;
    private readonly ILogger<GeminiLlmClient> _logger;

    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public string ProviderName => "Gemini";

    public GeminiLlmClient(IHttpClientFactory http, IConfiguration config, ILogger<GeminiLlmClient> logger)
    {
        _http = http;
        _logger = logger;

        var section = config.GetSection("AI:Gemini");
        _apiKey    = section["ApiKey"]    ?? throw new InvalidOperationException("AI:Gemini:ApiKey is missing.");
        _ocrModel  = section["OcrModel"]  ?? "gemini-1.5-pro";
        _chatModel = section["ChatModel"] ?? "gemini-2.0-flash";
    }

    public Task<string?> CompleteAsync(string systemPrompt, string userMessage,
        int maxTokens = 1000, CancellationToken ct = default)
        => CallAsync(_chatModel, systemPrompt, userMessage, null, null, maxTokens, ct);

    public Task<string?> VisionAsync(string systemPrompt, string userMessage,
        byte[] imageBytes, string mimeType, int maxTokens = 1500, CancellationToken ct = default)
        => CallAsync(_ocrModel, systemPrompt, userMessage, imageBytes, mimeType, maxTokens, ct);

    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string?> CallAsync(string model, string systemPrompt, string userMessage,
        byte[]? imageBytes, string? mimeType, int maxTokens, CancellationToken ct)
    {
        try
        {
            // Build user parts
            var parts = new List<object>();

            if (imageBytes is not null && mimeType is not null)
            {
                var base64 = Convert.ToBase64String(imageBytes);
                parts.Add(new { inline_data = new { mime_type = mimeType, data = base64 } });
            }

            parts.Add(new { text = userMessage });

            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents           = new[] { new { role = "user", parts } },
                generationConfig   = new { maxOutputTokens = maxTokens, temperature = 0.3 }
            };

            var url = $"{BaseUrl}/{model}:generateContent?key={_apiKey}";

            using var client  = _http.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Gemini] {Status}: {Body}", response.StatusCode,
                    await response.Content.ReadAsStringAsync(ct));
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Gemini] Call failed");
            return null;
        }
    }
}
