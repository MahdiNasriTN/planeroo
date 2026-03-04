using Planeroo.Application.DTOs.AI;
using Planeroo.Application.DTOs.Timetable;
using Planeroo.Domain.Common;

namespace Planeroo.Application.Interfaces;

public interface ITimetableService
{
    Task<Result<ScanTimetableResultDto>> ScanTimetableAsync(Guid childId, Stream imageStream, string fileName, CancellationToken ct = default);
    Task<Result<TimetableDto>> ConfirmTimetableAsync(ConfirmTimetableRequest request, CancellationToken ct = default);
    Task<Result<TimetableDto?>> GetTimetableAsync(Guid childId, CancellationToken ct = default);
    Task<Result<TimetableEntryDto>> UpdateEntryAsync(Guid entryId, ScannedTimetableEntryDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteEntryAsync(Guid entryId, CancellationToken ct = default);
}

public interface IOcrService
{
    Task<Result<ScanResultDto>> ProcessImageAsync(Guid childId, Stream imageStream, string fileName, CancellationToken ct = default);
    Task<Result<List<DetectedHomeworkDto>>> ExtractHomeworksAsync(string ocrText, CancellationToken ct = default);
    Task<Result<int>> ConfirmScanAsync(ConfirmScanRequest request, CancellationToken ct = default);
}

public interface IAIService
{
    Task<Result<AIChatResponse>> ChatAsync(AIChatRequest request, CancellationToken ct = default);
    Task<Result<StudySheetDto>> GenerateStudySheetAsync(GenerateStudySheetRequest request, CancellationToken ct = default);
    Task<Result<List<StudySheetDto>>> GetStudySheetsAsync(Guid childId, CancellationToken ct = default);
    Task<Result<string>> GeneratePlanningAdviceAsync(Guid childId, CancellationToken ct = default);
    Task<bool> IsContentSafeAsync(string content, CancellationToken ct = default);
}

public interface IPlanningEngine
{
    Task<Result<DTOs.Planning.WeeklyPlanningDto>> GenerateWeeklyPlanAsync(
        DTOs.Planning.GeneratePlanningRequest request, CancellationToken ct = default);
    Task<Result<DTOs.Planning.WeeklyPlanningDto>> RebalancePlanAsync(
        Guid childId, int weekNumber, int year, CancellationToken ct = default);
}

public interface IGamificationService
{
    Task<Result<DTOs.Gamification.GamificationProfileDto>> GetProfileAsync(Guid childId, CancellationToken ct = default);
    Task<Result<DTOs.Gamification.LevelUpDto?>> AddXpAsync(Guid childId, int amount, string reason, CancellationToken ct = default);
    Task CheckAndAwardBadgesAsync(Guid childId, CancellationToken ct = default);
    Task UpdateStreakAsync(Guid childId, CancellationToken ct = default);
    Task<string> GetMotivationalMessageAsync(Guid childId, CancellationToken ct = default);
    Task<string> GetMascotMoodAsync(Guid childId, CancellationToken ct = default);
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string containerName, CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string blobUrl, CancellationToken ct = default);
    Task DeleteAsync(string blobUrl, CancellationToken ct = default);
}

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string token, CancellationToken ct = default);
    Task SendWeeklyReportAsync(Guid parentId, CancellationToken ct = default);
    Task SendNotificationEmailAsync(string email, string subject, string body, CancellationToken ct = default);
}

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Role { get; }
    bool IsParent { get; }
    bool IsChild { get; }
}
