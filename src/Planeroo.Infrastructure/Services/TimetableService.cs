using System.Text.Json;
using Microsoft.Extensions.Logging;
using Planeroo.Application.DTOs.Timetable;
using Planeroo.Application.Interfaces;
using Planeroo.Domain.Common;
using Planeroo.Domain.Entities;
using Planeroo.Domain.Interfaces;

namespace Planeroo.Infrastructure.Services;

public class TimetableService : ITimetableService
{
    private readonly ILlmClient _llm;
    private readonly IRepository<ChildTimetable> _timetables;
    private readonly IRepository<TimetableEntry> _entries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TimetableService> _logger;

    public TimetableService(
        ILlmClient llm,
        IRepository<ChildTimetable> timetables,
        IRepository<TimetableEntry> entries,
        IUnitOfWork unitOfWork,
        ILogger<TimetableService> logger)
    {
        _llm        = llm;
        _timetables = timetables;
        _entries    = entries;
        _unitOfWork = unitOfWork;
        _logger     = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scan
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result<ScanTimetableResultDto>> ScanTimetableAsync(
        Guid childId, Stream imageStream, string fileName, CancellationToken ct = default)
    {
        try
        {
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms, ct);
            var imageBytes = ms.ToArray();
            var mimeType   = GetMimeType(fileName);

            const string systemPrompt =
                "You are a specialized OCR system for reading school weekly timetables (emplois du temps). " +
                "You extract subjects, days and time slots from timetable images, including Arabic timetables. " +
                "Return ONLY valid JSON with no markdown, no explanations, no code blocks.";

            const string userPrompt = @"Analyze this school timetable image.
Extract each class slot with its day, time, and subject.

IMPORTANT — SKIP breaks/recess: any cell containing فسحة, استراحة, Break, Recess, Pause must be OMITTED from entries.

Subject mapping (normalize to English key + keep original display name):
Arabic subjects:
- عربية / لغة عربية / اللغة العربية → subject: ""Arabic"", subjectDisplayName: ""عربية""
- رياضيات / رياضة → subject: ""Mathematics"", subjectDisplayName: ""رياضيات""
- فرنسية / لغة فرنسية → subject: ""French"", subjectDisplayName: ""فرنسية""
- إنقليزية / إنجليزية / لغة إنجليزية → subject: ""English"", subjectDisplayName: ""إنقليزية""
- علوم فيزيائية / فيزيائية / فيزياء / كيمياء → subject: ""Physics"", subjectDisplayName: ""علوم فيزيائية""
- علوم الحياة والأرض / علوم طبيعية / أحياء / بيولوجيا → subject: ""Biology"", subjectDisplayName: ""علوم الحياة والأرض""
- علوم / العلوم → subject: ""Science"", subjectDisplayName: ""علوم""
- تاريخ وجغرافيا / تاريخ / جغرافيا → subject: ""History"", subjectDisplayName: ""تاريخ وجغرافيا""
- تكنولوجيا / تكنولوجيا المعلومات → subject: ""Technology"", subjectDisplayName: ""تكنولوجيا""
- إعلامية / إعلام آلي / معلوماتية → subject: ""Technology"", subjectDisplayName: ""إعلامية""
- تربية مدنية / تربية وطنية → subject: ""Civic"", subjectDisplayName: ""تربية مدنية""
- تربية بدنية / EPS / رياضة بدنية → subject: ""Sport"", subjectDisplayName: ""تربية بدنية""
- تربية فنية / رسم / فنون → subject: ""Art"", subjectDisplayName: ""تربية فنية""
- موسيقى / تربية موسيقية → subject: ""Music"", subjectDisplayName: ""موسيقى""
- فلسفة → subject: ""Philosophy"", subjectDisplayName: ""فلسفة""
French/other subjects:
- Mathématiques / Maths → subject: ""Mathematics"", subjectDisplayName: ""Mathématiques""
- Français → subject: ""French"", subjectDisplayName: ""Français""
- Anglais → subject: ""English"", subjectDisplayName: ""Anglais""
- Sciences / SVT / Physique-Chimie → subject: ""Science"", subjectDisplayName: use exact text
- Histoire-Géographie / Histoire / Géographie → subject: ""History"", subjectDisplayName: use exact text
- Informatique / TIC → subject: ""Technology"", subjectDisplayName: use exact text
- Anything else → subject: ""Other"", subjectDisplayName: use exact text from image

For days use English: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday.
For period: Morning (before 12:00), Afternoon (12:00 and after).
For subjectDisplayName: use the EXACT text as it appears in the image cell.

OUTPUT FORMAT (JSON only, no markdown):
{
  ""rawText"": ""full text extracted from image"",
  ""entries"": [
    {
      ""dayOfWeek"": ""Monday"",
      ""startTime"": ""08:00"",
      ""endTime"": ""09:00"",
      ""subject"": ""Arabic"",
      ""subjectDisplayName"": ""عربية"",
      ""period"": ""Morning"",
      ""confidence"": 0.95
    }
  ],
  ""confidenceScore"": 0.90
}
If the image does not contain a timetable, return { ""rawText"": """", ""entries"": [], ""confidenceScore"": 0 }.";

            _logger.LogInformation("[TimetableScan] Calling vision for child {ChildId}, file={File}", childId, fileName);

            var rawContent = await _llm.VisionAsync(systemPrompt, userPrompt, imageBytes, mimeType, 3000, ct);

            _logger.LogDebug("[TimetableScan] Raw response ({Len} chars): {Preview}",
                rawContent?.Length ?? 0, rawContent?[..Math.Min(500, rawContent?.Length ?? 0)] ?? "<null>");

            if (string.IsNullOrWhiteSpace(rawContent))
                return Result<ScanTimetableResultDto>.Failure("LLM returned empty response", 500);

            var json = ExtractJson(rawContent);

            ScanTimetableResultDto? parsed = null;
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var rawText         = root.TryGetProperty("rawText", out var rt) ? rt.GetString() ?? "" : "";
                var confidenceScore = root.TryGetProperty("confidenceScore", out var cs) ? cs.GetDouble() : 0.0;

                var detectedEntries = new List<ScannedTimetableEntryDto>();
                if (root.TryGetProperty("entries", out var entriesEl) && entriesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in entriesEl.EnumerateArray())
                    {
                        detectedEntries.Add(new ScannedTimetableEntryDto(
                            DayOfWeek:          entry.TryGetProperty("dayOfWeek",         out var d) ? d.GetString() ?? "" : "",
                            StartTime:          entry.TryGetProperty("startTime",          out var st) ? st.GetString() ?? "" : "",
                            EndTime:            entry.TryGetProperty("endTime",            out var et) ? et.GetString() ?? "" : "",
                            Subject:            entry.TryGetProperty("subject",            out var s) ? s.GetString() ?? "" : "",
                            SubjectDisplayName: entry.TryGetProperty("subjectDisplayName", out var sdn) ? sdn.GetString() ?? "" : "",
                            Period:             entry.TryGetProperty("period",             out var p) ? p.GetString() : null,
                            Confidence:         entry.TryGetProperty("confidence",         out var c) ? c.GetDouble() : 0.8
                        ));
                    }
                }

                parsed = new ScanTimetableResultDto(
                    RawText: rawText,
                    DetectedEntries: detectedEntries,
                    ConfidenceScore: confidenceScore,
                    ProcessedAt: DateTime.UtcNow
                );
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[TimetableScan] Failed to parse JSON response");
                return Result<ScanTimetableResultDto>.Failure("Failed to parse timetable from image", 422);
            }

            return Result<ScanTimetableResultDto>.Success(parsed!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TimetableScan] Unexpected error for child {ChildId}", childId);
            return Result<ScanTimetableResultDto>.Failure($"Timetable scan failed: {ex.Message}", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Confirm (save / replace)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result<TimetableDto>> ConfirmTimetableAsync(
        ConfirmTimetableRequest request, CancellationToken ct = default)
    {
        try
        {
            // Remove existing timetable for this child (soft-delete entries + timetable)
            var allTimetables = await _timetables.GetAllAsync(ct);
            var existing = allTimetables.FirstOrDefault(t => t.ChildId == request.ChildId && !t.IsDeleted);
            if (existing is not null)
            {
                var existingEntries = await _entries.GetAllAsync(ct);
                foreach (var e in existingEntries.Where(e => e.TimetableId == existing.Id && !e.IsDeleted))
                {
                    e.IsDeleted = true;
                    await _entries.UpdateAsync(e, ct);
                }
                existing.IsDeleted = true;
                await _timetables.UpdateAsync(existing, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            // Create new timetable
            var timetable = new ChildTimetable { ChildId = request.ChildId };
            await _timetables.AddAsync(timetable, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // Add entries
            var savedEntries = new List<TimetableEntry>();
            foreach (var dto in request.Entries)
            {
                var entry = new TimetableEntry
                {
                    TimetableId        = timetable.Id,
                    DayOfWeek          = dto.DayOfWeek,
                    StartTime          = dto.StartTime,
                    EndTime            = dto.EndTime,
                    Subject            = dto.Subject,
                    SubjectDisplayName = dto.SubjectDisplayName,
                    Period             = dto.Period
                };
                await _entries.AddAsync(entry, ct);
                savedEntries.Add(entry);
            }
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<TimetableDto>.Success(MapToDto(timetable, savedEntries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TimetableConfirm] Failed for child {ChildId}", request.ChildId);
            return Result<TimetableDto>.Failure($"Failed to save timetable: {ex.Message}", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Get
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result<TimetableDto?>> GetTimetableAsync(Guid childId, CancellationToken ct = default)
    {
        try
        {
            var allTimetables = await _timetables.GetAllAsync(ct);
            var timetable = allTimetables.FirstOrDefault(t => t.ChildId == childId && !t.IsDeleted);
            if (timetable is null)
                return Result<TimetableDto?>.Success(null);

            var allEntries = await _entries.GetAllAsync(ct);
            var entries = allEntries.Where(e => e.TimetableId == timetable.Id && !e.IsDeleted).ToList();

            return Result<TimetableDto?>.Success(MapToDto(timetable, entries)!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TimetableGet] Failed for child {ChildId}", childId);
            return Result<TimetableDto?>.Failure($"Failed to retrieve timetable: {ex.Message}", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Update entry
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result<TimetableEntryDto>> UpdateEntryAsync(
        Guid entryId, ScannedTimetableEntryDto dto, CancellationToken ct = default)
    {
        try
        {
            var allEntries = await _entries.GetAllAsync(ct);
            var entry = allEntries.FirstOrDefault(e => e.Id == entryId && !e.IsDeleted);
            if (entry is null)
                return Result<TimetableEntryDto>.Failure("Entry not found", 404);

            entry.DayOfWeek          = dto.DayOfWeek;
            entry.StartTime          = dto.StartTime;
            entry.EndTime            = dto.EndTime;
            entry.Subject            = dto.Subject;
            entry.SubjectDisplayName = dto.SubjectDisplayName;
            entry.Period             = dto.Period;

            await _entries.UpdateAsync(entry, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<TimetableEntryDto>.Success(MapEntryToDto(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TimetableUpdateEntry] Failed for entry {EntryId}", entryId);
            return Result<TimetableEntryDto>.Failure($"Failed to update entry: {ex.Message}", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Delete entry
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result<bool>> DeleteEntryAsync(Guid entryId, CancellationToken ct = default)
    {
        try
        {
            var allEntries = await _entries.GetAllAsync(ct);
            var entry = allEntries.FirstOrDefault(e => e.Id == entryId && !e.IsDeleted);
            if (entry is null)
                return Result<bool>.Failure("Entry not found", 404);

            entry.IsDeleted = true;
            await _entries.UpdateAsync(entry, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TimetableDeleteEntry] Failed for entry {EntryId}", entryId);
            return Result<bool>.Failure($"Failed to delete entry: {ex.Message}", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static TimetableDto MapToDto(ChildTimetable t, List<TimetableEntry> entries)
        => new(
            Id:        t.Id,
            ChildId:   t.ChildId,
            CreatedAt: t.CreatedAt,
            Entries:   entries.Select(MapEntryToDto).ToList()
        );

    private static TimetableEntryDto MapEntryToDto(TimetableEntry e)
        => new(
            Id:                 e.Id,
            DayOfWeek:          e.DayOfWeek,
            StartTime:          e.StartTime,
            EndTime:            e.EndTime,
            Subject:            e.Subject,
            SubjectDisplayName: e.SubjectDisplayName,
            Period:             e.Period
        );

    private static string ExtractJson(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("```"))
        {
            var firstNewline = raw.IndexOf('\n');
            if (firstNewline >= 0) raw = raw[(firstNewline + 1)..];
            var lastFence = raw.LastIndexOf("```");
            if (lastFence >= 0) raw = raw[..lastFence];
        }
        return raw.Trim();
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            _                 => "image/jpeg"
        };
    }
}
