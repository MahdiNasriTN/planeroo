using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Planeroo.Application.Interfaces;

namespace Planeroo.Infrastructure.Services.LlmProviders;

/// <summary>
/// Anthropic (Claude) provider — uses the Messages API.
/// Supports Claude 3 / Claude 3.5 models with vision.
/// </summary>
public class AnthropicLlmClient : ILlmClient
{
    private readonly IHttpClientFactory _http;
    private readonly string _apiKey;
    private readonly string _ocrModel;
    private readonly string _chatModel;
    private readonly ILogger<AnthropicLlmClient> _logger;

    private const string ApiVersion = "2023-06-01";
    private const string BaseUrl    = "https://api.anthropic.com/v1/messages";

    public string ProviderName => "Anthropic";

    public AnthropicLlmClient(IHttpClientFactory http, IConfiguration config, ILogger<AnthropicLlmClient> logger)
    {
        _http = http;
        _logger = logger;

        var section = config.GetSection("AI:Anthropic");
        _apiKey    = section["ApiKey"]    ?? throw new InvalidOperationException("AI:Anthropic:ApiKey is missing.");
        _ocrModel  = section["OcrModel"]  ?? "claude-3-5-sonnet-20241022";
        _chatModel = section["ChatModel"] ?? "claude-3-haiku-20240307";
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
            object[] userContent;
            if (imageBytes is not null && mimeType is not null)
            {
                var base64 = Convert.ToBase64String(imageBytes);
                userContent =
                [
                    new
                    {
                        type   = "image",
                        source = new { type = "base64", media_type = mimeType, data = base64 }
                    },
                    new { type = "text", text = userMessage }
                ];
            }
            else
            {
                userContent = [new { type = "text", text = userMessage }];
            }

            var payload = new
            {
                model,
                max_tokens = maxTokens,
                system     = systemPrompt,
                messages   = new[] { new { role = "user", content = userContent } }
            };

            using var client  = _http.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", ApiVersion);

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Anthropic] {Status}: {Body}", response.StatusCode,
                    await response.Content.ReadAsStringAsync(ct));
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Anthropic] Call failed");
            return null;
        }
    }
}
