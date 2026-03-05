using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Planeroo.Application.Interfaces;

namespace Planeroo.Infrastructure.Services.LlmProviders;

/// <summary>
/// OpenAI provider — uses Chat Completions API with optional vision.
/// Supports any GPT-4o / GPT-4-vision model.
/// </summary>
public class OpenAiLlmClient : ILlmClient
{
    private readonly IHttpClientFactory _http;
    private readonly string _apiKey;
    private readonly string _ocrModel;
    private readonly string _chatModel;
    private readonly ILogger<OpenAiLlmClient> _logger;

    public string ProviderName => "OpenAI";

    public OpenAiLlmClient(IHttpClientFactory http, IConfiguration config, ILogger<OpenAiLlmClient> logger)
    {
        _http = http;
        _logger = logger;

        var section = config.GetSection("AI:OpenAI");
        _apiKey   = section["ApiKey"]    ?? throw new InvalidOperationException("AI:OpenAI:ApiKey is missing.");
        _ocrModel = section["OcrModel"]  ?? "gpt-4o";
        _chatModel = section["ChatModel"] ?? "gpt-4o-mini";
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
            object userContent;
            if (imageBytes is not null && mimeType is not null)
            {
                var base64 = Convert.ToBase64String(imageBytes);
                userContent = new object[]
                {
                    new { type = "text", text = userMessage },
                    new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64}", detail = "high" } }
                };
            }
            else
            {
                userContent = userMessage;
            }

            var payload = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userContent  }
                },
                max_tokens  = maxTokens,
                temperature = 0.3
            };

            using var client  = _http.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[OpenAI] {Status}: {Body}", response.StatusCode,
                    await response.Content.ReadAsStringAsync(ct));
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OpenAI] Call failed");
            return null;
        }
    }
}
