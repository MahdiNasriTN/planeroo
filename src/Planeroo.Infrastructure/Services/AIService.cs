using Microsoft.Extensions.Logging;
using Planeroo.Application.DTOs.AI;
using Planeroo.Application.Interfaces;
using Planeroo.Domain.Common;
using Planeroo.Domain.Entities;
using Planeroo.Domain.Enums;
using Planeroo.Domain.Interfaces;

namespace Planeroo.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly ILlmClient _llm;
    private readonly ILogger<AIService> _logger;
    private readonly IRepository<StudySheet> _studySheets;
    private readonly IUnitOfWork _unitOfWork;

    // Fast local content filter — no API cost
    private static readonly string[] _blocked =
    [
        "violence", "weapon", "kill", "drug", "porn", "adult",
        "haine", "arme", "tuer", "drogue"
    ];

    public AIService(ILlmClient llm, ILogger<AIService> logger,
        IRepository<StudySheet> studySheets, IUnitOfWork unitOfWork)
    {
        _llm         = llm;
        _logger      = logger;
        _studySheets = studySheets;
        _unitOfWork  = unitOfWork;
    }

    public async Task<Result<AIChatResponse>> ChatAsync(AIChatRequest request, CancellationToken ct = default)
    {
        if (!await IsContentSafeAsync(request.Message, ct))
        {
            return Result<AIChatResponse>.Success(new AIChatResponse(
                Message: "Je suis là pour t'aider avec tes devoirs scolaires ! Pose-moi une question sur les mathématiques, les sciences, le français ou d'autres matières.",
                WasFiltered: true,
                Topic: request.Topic,
                MascotReaction: "confused"
            ));
        }

        const string systemPrompt = """
            Tu es Plani, un assistant pédagogique bienveillant et encourageant pour les enfants de 6 à 16 ans.
            Règles strictes :
            - Réponds TOUJOURS en français, de manière simple et positive.
            - Aide uniquement avec les sujets scolaires (maths, sciences, langues, histoire, géographie, art, musique).
            - Sois enthousiaste et utilise des emojis adaptés aux enfants.
            - Si la question sort des sujets scolaires, redirige gentiment.
            - Donne des explications courtes et claires, avec des exemples concrets.
            """;

        var userMessage = request.Topic is not null
            ? $"{request.Message}\n[Sujet actuel : {request.Topic}]"
            : request.Message;

        var reply = await _llm.CompleteAsync(systemPrompt, userMessage, 1000, ct);

        if (reply is null)
        {
            _logger.LogWarning("[{Provider}] Chat returned null", _llm.ProviderName);
            return Result<AIChatResponse>.Failure("Le service IA est temporairement indisponible.", 503);
        }

        return Result<AIChatResponse>.Success(new AIChatResponse(
            Message: reply,
            WasFiltered: false,
            Topic: request.Topic,
            MascotReaction: "happy"
        ));
    }

    public async Task<Result<StudySheetDto>> GenerateStudySheetAsync(GenerateStudySheetRequest request, CancellationToken ct = default)
    {
        var age = request.TargetAge ?? 11;
        var systemPrompt = $"""
            Tu es un professeur expert en création de fiches pédagogiques pour enfants de {age} ans.
            Génère des fiches claires, structurées avec des titres, des définitions simples et des exemples.
            Utilise des listes à puces, des emojis et un ton encourageant. Réponds toujours en français.
            """;

        var userPrompt = $"Crée une fiche de révision complète sur : {request.Topic} en {request.Subject}. " +
                         $"Public : {age} ans.";

        var content = await _llm.CompleteAsync(systemPrompt, userPrompt, 1200, ct);

        if (content is null)
            return Result<StudySheetDto>.Failure("Le service IA est temporairement indisponible.", 503);

        var summary = content.Length > 300 ? content[..300].TrimEnd() + "…" : content;
        _ = Enum.TryParse<SubjectType>(request.Subject, ignoreCase: true, out var subjectEnum);

        var entity = new StudySheet
        {
            ChildId    = request.ChildId,
            HomeworkId = request.HomeworkId,
            Title      = $"{request.Topic} — {request.Subject}",
            Subject    = subjectEnum,
            Content    = content,
            Summary    = summary,
            TargetAge  = age,
            GradeLevel = Math.Max(1, age - 5),
            IsFavorite = false,
            ViewCount  = 0,
        };

        await _studySheets.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<StudySheetDto>.Success(new StudySheetDto(
            Id: entity.Id,
            ChildId: entity.ChildId,
            Title: entity.Title,
            Subject: entity.Subject.ToString(),
            Content: entity.Content,
            Summary: entity.Summary,
            TargetAge: entity.TargetAge,
            GradeLevel: entity.GradeLevel,
            IsFavorite: entity.IsFavorite,
            ViewCount: entity.ViewCount,
            CreatedAt: entity.CreatedAt
        ));
    }

    public async Task<Result<List<StudySheetDto>>> GetStudySheetsAsync(Guid childId, CancellationToken ct = default)
    {
        var all = await _studySheets.GetAllAsync(ct);
        var sheets = all
            .Where(s => s.ChildId == childId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StudySheetDto(
                Id: s.Id,
                ChildId: s.ChildId,
                Title: s.Title,
                Subject: s.Subject.ToString(),
                Content: s.Content,
                Summary: s.Summary,
                TargetAge: s.TargetAge,
                GradeLevel: s.GradeLevel,
                IsFavorite: s.IsFavorite,
                ViewCount: s.ViewCount,
                CreatedAt: s.CreatedAt
            ))
            .ToList();

        return Result<List<StudySheetDto>>.Success(sheets);
    }

    public async Task<Result<string>> GeneratePlanningAdviceAsync(Guid childId, CancellationToken ct = default)
    {
        const string system = "Tu es un conseiller en organisation scolaire pour enfants. Sois concis et pratique.";
        const string prompt = "Donne exactement 3 conseils courts pour bien organiser ses devoirs de la semaine. " +
                              "Réponds en JSON : {\"advice\": [\"conseil1\", \"conseil2\", \"conseil3\"]}";

        var result = await _llm.CompleteAsync(system, prompt, 300, ct);

        return Result<string>.Success(result
            ?? "{\"advice\": [\"Commence par les devoirs les plus difficiles.\", \"Fais des pauses de 10 min.\", \"Révise chaque soir 30 minutes.\"]}");
    }

    public Task<bool> IsContentSafeAsync(string content, CancellationToken ct = default)
    {
        var lower  = content.ToLowerInvariant();
        var isSafe = !_blocked.Any(k => lower.Contains(k));
        return Task.FromResult(isSafe);
    }
}
