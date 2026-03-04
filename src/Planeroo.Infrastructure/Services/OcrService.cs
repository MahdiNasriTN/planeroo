using System.Text.Json;
using Microsoft.Extensions.Logging;
using Planeroo.Application.DTOs.AI;
using Planeroo.Application.Interfaces;
using Planeroo.Domain.Common;
using Planeroo.Domain.Entities;
using Planeroo.Domain.Enums;
using Planeroo.Domain.Interfaces;

namespace Planeroo.Infrastructure.Services;

public class OcrService : IOcrService
{
    private readonly ILlmClient _llm;
    private readonly IRepository<ScanSession> _sessions;
    private readonly IRepository<Homework> _homeworks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OcrService> _logger;

    public OcrService(
        ILlmClient llm,
        IRepository<ScanSession> sessions,
        IRepository<Homework> homeworks,
        IUnitOfWork unitOfWork,
        ILogger<OcrService> logger)
    {
        _llm        = llm;
        _sessions   = sessions;
        _homeworks  = homeworks;
        _unitOfWork = unitOfWork;
        _logger     = logger;
    }

    public async Task<Result<ScanResultDto>> ProcessImageAsync(Guid childId, Stream imageStream, string fileName, CancellationToken ct = default)
    {
        var session = new ScanSession
        {
            ChildId  = childId,
            ImageUrl = fileName,
            Status   = ScanStatus.Processing,
        };
        await _sessions.AddAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms, ct);
            var imageBytes = ms.ToArray();
            var mimeType   = GetMimeType(fileName);

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            const string systemPrompt =
                "Tu es un système OCR intelligent spécialisé dans l'extraction de devoirs ET d'examens scolaires. " +
                "Tu sais lire aussi bien des photos d'agenda que des emplois du temps d'examens, des tableaux de contrôles, " +
                "ou tout document scolaire listant des matières avec des dates. " +
                "IMPORTANT : tu dois TOUJOURS extraire TOUTES les entrées visibles dans l'image, sans en omettre aucune. " +
                "Retourne uniquement du JSON valide, sans explications ni balises markdown.";

            var userPrompt = $@"Date d'aujourd'hui : {today}

Analyse cette image scolaire. Elle peut être :
- Un agenda avec des devoirs écrits
- Un emploi du temps d'examen ou de contrôle (tableau EMPLOI D'EXAMEN / EMPLOI DE CONTROLE)
- Un calendrier de contrôles/tests
- Toute autre source listant des matières à préparer

RÈGLES IMPORTANTES :
1. Si tu vois un tableau d'examens/contrôles (type EMPLOI D'EXAMEN), chaque cellule = une entrée séparée.
   Parcours TOUTES les lignes et TOUTES les colonnes du tableau.
2. Pour chaque examen/contrôle, titre = ""Révision [matière]"", estimatedMinutes = 90 ou 120.
3. Ne jamais retourner tasks:[] si tu vois du texte scolaire dans l'image.
4. Si tu n'es pas sûr de la matière, utilise ""Other"".

Correspondance des matières :
- Mathématiques / Math / Maths → Mathematics
- Français / Rédaction / Expression → French
- Anglais / English / Langue étrangère → English
- Sciences / Physique / Chimie / SVT / Biologie / Science de la vie → Science
- Histoire / Géographie / Histoire-Géo → History
- Informatique / Info / TIC → Technology
- Art / Dessin / Arts plastiques → Art
- Musique / Éducation musicale → Music
- Technologie → Technology
- Arabe / Arab / اللغة العربية → Other
- EPS / Sport → Other
- Éducation Civique / Civique → Other
- Tout le reste → Other

Pour les jours de la semaine sans date précise (Lundi/Monday, Mardi/Tuesday...) :
- Calcule la date réelle à partir d'aujourd'hui ({today})
- Si le jour est déjà passé cette semaine, prends le même jour la semaine PROCHAINE

Retourne un objet JSON avec exactement cette structure :
{{
  ""rawText"": ""texte brut extrait de l'image"",
  ""tasks"": [
    {{
      ""title"": ""Révision Mathématiques"",
      ""description"": null,
      ""subject"": ""Mathematics"",
      ""dueDate"": ""YYYY-MM-DD"",
      ""estimatedMinutes"": 90,
      ""confidence"": 0.95
    }}
  ]
}}
Si et seulement si l'image ne contient AUCUN contenu scolaire, retourne {{""rawText"": """", ""tasks"": []}}.";


            _logger.LogInformation("[OCR] Calling {Provider} VisionAsync for child {ChildId}, file={File}, size={Size}bytes",
                _llm.ProviderName, childId, fileName, imageBytes.Length);

            var rawContent = await _llm.VisionAsync(systemPrompt, userPrompt, imageBytes, mimeType, 3000, ct);

            _logger.LogDebug("[OCR] Raw LLM response ({Length} chars): {Preview}",
                rawContent?.Length ?? 0,
                rawContent?[..Math.Min(500, rawContent.Length)] ?? "<null>");

            if (rawContent is null)
            {
                _logger.LogError("LLM provider {Provider} returned null for OCR", _llm.ProviderName);
                session.Status       = ScanStatus.Failed;
                session.ErrorMessage = $"No response from {_llm.ProviderName}.";
                await _sessions.UpdateAsync(session, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result<ScanResultDto>.Failure("OCR processing failed — provider returned no response.", 502);
            }

            var jsonContent = ExtractJsonObject(rawContent);
            using var parsed = JsonDocument.Parse(jsonContent);
            var root = parsed.RootElement;

            var rawText = root.TryGetProperty("rawText", out var rt) ? rt.GetString() ?? "" : "";
            var tasks = new List<DetectedHomeworkDto>();

            if (root.TryGetProperty("tasks", out var tasksEl))
            {
                foreach (var t in tasksEl.EnumerateArray())
                {
                    DateTime? dueDate = null;
                    if (t.TryGetProperty("dueDate", out var ddEl)
                        && ddEl.ValueKind != JsonValueKind.Null
                        && DateTime.TryParse(ddEl.GetString(), out var parsedDate))
                    {
                        dueDate = parsedDate;
                    }

                    tasks.Add(new DetectedHomeworkDto(
                        Title:            t.TryGetProperty("title",            out var title) ? title.GetString() ?? "Devoir" : "Devoir",
                        Description:      t.TryGetProperty("description",      out var desc)  && desc.ValueKind != JsonValueKind.Null ? desc.GetString() : null,
                        Subject:          t.TryGetProperty("subject",          out var subj)  ? subj.GetString() ?? "Other" : "Other",
                        DueDate:          dueDate,
                        EstimatedMinutes: t.TryGetProperty("estimatedMinutes", out var em)   ? em.GetInt32()    : 30,
                        Confidence:       t.TryGetProperty("confidence",       out var conf)  ? conf.GetDouble() : 0.8
                    ));
                }
            }

            session.RawOcrText         = rawText;
            session.Status             = tasks.Count > 0 ? ScanStatus.Completed : ScanStatus.RequiresReview;
            session.DetectedTasksCount = tasks.Count;
            session.ConfidenceScore    = tasks.Count > 0 ? tasks.Average(t => t.Confidence) : 0;
            session.ProcessedAt        = DateTime.UtcNow;

            _logger.LogInformation("[OCR] Detected {Count} tasks for child {ChildId}", tasks.Count, childId);

            if (tasks.Count == 0 && rawText.Length > 10)
                _logger.LogWarning("[OCR] Zero tasks but rawText is non-empty ({RawLen} chars). LLM may not have recognized the document type. RawText start: {Start}",
                    rawText.Length, rawText[..Math.Min(200, rawText.Length)]);
            await _sessions.UpdateAsync(session, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<ScanResultDto>.Success(new ScanResultDto(
                ScanSessionId:   session.Id,
                Status:          session.Status.ToString(),
                RawText:         rawText,
                ConfidenceScore: session.ConfidenceScore,
                DetectedTasks:   tasks,
                ProcessedAt:     session.ProcessedAt.Value
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR processing error for child {ChildId}", childId);
            session.Status = ScanStatus.Failed;
            session.ErrorMessage = ex.Message;
            await _sessions.UpdateAsync(session, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<ScanResultDto>.Failure($"OCR error: {ex.Message}", 500);
        }
    }

    public async Task<Result<int>> ConfirmScanAsync(ConfirmScanRequest request, CancellationToken ct = default)
    {
        try
        {
            var session = await _sessions.GetByIdAsync(request.ScanSessionId, ct);
            if (session is null)
                return Result<int>.Failure("Scan session not found.", 404);

            int created = 0;
            foreach (var task in request.Tasks)
            {
                if (string.IsNullOrWhiteSpace(task.Title)) continue;

                var subject = Enum.TryParse<SubjectType>(task.Subject, true, out var subjectEnum)
                    ? subjectEnum : SubjectType.Other;

                var homework = new Homework
                {
                    ChildId = session.ChildId,
                    ScanSessionId = session.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Subject = subject,
                    DueDate = task.DueDate,
                    EstimatedMinutes = task.EstimatedMinutes > 0 ? task.EstimatedMinutes : 30,
                    IsAutoDetected = true,
                    Status = HomeworkStatus.Pending,
                    Priority = HomeworkPriority.Medium,
                    XpReward = 10,
                };

                await _homeworks.AddAsync(homework, ct);
                created++;
            }

            session.ConfirmedTasksCount = created;
            await _sessions.UpdateAsync(session, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<int>.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmScan error for session {SessionId}", request.ScanSessionId);
            return Result<int>.Failure($"Confirm error: {ex.Message}", 500);
        }
    }

    public Task<Result<List<DetectedHomeworkDto>>> ExtractHomeworksAsync(string ocrText, CancellationToken ct = default)
    {
        // Fallback: parse plain text line-by-line looking for homework patterns
        var tasks = new List<DetectedHomeworkDto>();
        var lines = ocrText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 5)
            {
                tasks.Add(new DetectedHomeworkDto(
                    Title: trimmed,
                    Description: null,
                    Subject: "Other",
                    DueDate: null,
                    EstimatedMinutes: 30,
                    Confidence: 0.6
                ));
            }
        }
        return Task.FromResult(Result<List<DetectedHomeworkDto>>.Success(tasks));
    }

    private static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }
}
