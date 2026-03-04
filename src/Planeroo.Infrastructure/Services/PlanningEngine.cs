using System.Globalization;
using Microsoft.Extensions.Logging;
using Planeroo.Application.DTOs.Planning;
using Planeroo.Application.Interfaces;
using Planeroo.Domain.Common;
using Planeroo.Domain.Entities;
using Planeroo.Domain.Enums;
using Planeroo.Domain.Interfaces;

namespace Planeroo.Infrastructure.Services;

public class PlanningEngine : IPlanningEngine
{
    private readonly IRepository<Homework> _homeworks;
    private readonly IRepository<ChildTimetable> _timetables;
    private readonly IRepository<TimetableEntry> _timetableEntries;
    private readonly ILogger<PlanningEngine> _logger;

    // Default study windows per day (after school until evening)
    private static readonly TimeSpan DefaultStudyStart = new(16, 0, 0); // 16:00
    private static readonly TimeSpan DefaultStudyEnd   = new(20, 0, 0); // 20:00  (240 min → fits 2×90 min tasks)
    private const int BreakBetweenTasksMinutes = 10;

    public PlanningEngine(
        IRepository<Homework> homeworks,
        IRepository<ChildTimetable> timetables,
        IRepository<TimetableEntry> timetableEntries,
        ILogger<PlanningEngine> logger)
    {
        _homeworks        = homeworks;
        _timetables       = timetables;
        _timetableEntries = timetableEntries;
        _logger           = logger;
    }

    public async Task<Result<WeeklyPlanningDto>> GenerateWeeklyPlanAsync(
        GeneratePlanningRequest request, CancellationToken ct = default)
    {
        try
        {
            List<Homework> pending;
            List<Homework> completed;

            if (request.Source.Equals("timetable", StringComparison.OrdinalIgnoreCase))
            {
                // ── TIMETABLE SOURCE: build synthetic Homework objects from saved timetable ──
                var allTimetables = await _timetables.GetAllAsync(ct);
                var timetable = allTimetables.FirstOrDefault(t => t.ChildId == request.ChildId && !t.IsDeleted);

                if (timetable is null)
                {
                    _logger.LogWarning("[Planning] No timetable found for child {ChildId}", request.ChildId);
                    // Return empty plan gracefully
                    pending  = new List<Homework>();
                    completed = new List<Homework>();
                }
                else
                {
                    var allEntries = await _timetableEntries.GetAllAsync(ct);
                    var entries = allEntries
                        .Where(e => e.TimetableId == timetable.Id && !e.IsDeleted)
                        .ToList();

                    // One synthetic Homework per unique subject
                    pending = entries
                        .GroupBy(e => e.Subject)
                        .Select(g =>
                        {
                            var first = g.First();
                            if (!Enum.TryParse<SubjectType>(first.Subject, out var subjectEnum))
                                subjectEnum = SubjectType.Other;

                            return new Homework
                            {
                                Id               = Guid.NewGuid(),
                                ChildId          = request.ChildId,
                                Title            = $"Révision {first.SubjectDisplayName}",
                                Subject          = subjectEnum,
                                Status           = HomeworkStatus.Pending,
                                Priority         = HomeworkPriority.Medium,
                                EstimatedMinutes = 90,
                                DueDate          = DateTime.UtcNow.AddDays(7)
                            };
                        })
                        .ToList();

                    completed = new List<Homework>();
                }
            }
            else
            {
                // ── DEVOIRS SOURCE (default): fetch real pending homeworks ──
                var allHomeworks = await _homeworks.GetAllAsync(ct);
                pending = allHomeworks
                    .Where(h => h.ChildId == request.ChildId
                             && !h.IsDeleted
                             && h.Status is HomeworkStatus.Pending or HomeworkStatus.InProgress)
                    .OrderBy(h => h.DueDate)
                    .ThenByDescending(h => (int)h.Priority)
                    .ToList();

                completed = allHomeworks
                    .Where(h => h.ChildId == request.ChildId && !h.IsDeleted && h.Status == HomeworkStatus.Completed)
                    .ToList();
            }

            // Compute week start (Monday) — fall back to current week if caller sent 0
            var resolvedYear   = request.Year       > 0 ? request.Year       : ISOWeek.GetYear(DateTime.UtcNow);
            var resolvedWeek   = request.WeekNumber > 0 ? request.WeekNumber : ISOWeek.GetWeekOfYear(DateTime.UtcNow);
            var weekStart = ISOWeek.ToDateTime(resolvedYear, resolvedWeek, DayOfWeek.Monday);

            var days = new List<DayPlanDto>();
            int totalMinutes = 0;
            var queue = new Queue<Homework>(pending);

            // ── SMART MODE: pre-distribute tasks evenly across all 5 days ──
            Dictionary<int, List<Homework>>? smartDayMap = null;
            if (request.Mode.Equals("smart", StringComparison.OrdinalIgnoreCase))
            {
                smartDayMap = new();
                for (int i = 0; i < 5; i++) smartDayMap[i] = new();
                for (int i = 0; i < pending.Count; i++)
                    smartDayMap[i % 5].Add(pending[i]); // round-robin across Mon–Fri
            }

            for (int offset = 0; offset < 5; offset++) // Monday → Friday
            {
                var date = weekStart.AddDays(offset);
                var dotNetDow = date.DayOfWeek; // Sunday=0, Monday=1, ...
                var dayName = dotNetDow.ToString(); // "Monday", "Tuesday", …

                // Resolve available window for this day
                var (studyStart, studyEnd) = ResolveWindow(request.AvailableSlots, dotNetDow);
                var totalAvailableMinutes = (int)(studyEnd - studyStart).TotalMinutes;

                var slots = new List<PlanningSlotDto>();
                var usedMinutes = 0;
                int order = 0;

                // Determine which tasks go on this day based on mode
                List<Homework> orderedQueue;
                if (smartDayMap is not null)
                {
                    // Smart: pre-assigned tasks for this day slot
                    orderedQueue = smartDayMap[offset];
                }
                else
                {
                    // Quick / Custom: schedule tasks on (or before) their due date
                    // Add 6h before extracting .Date to compensate for dates stored as
                    // midnight-local (UTC+1..+5) which appear as the previous UTC day.
                    var urgentToday = queue
                        .Where(h => h.DueDate.AddHours(6).Date < date.Date)   // overdue
                        .ToList();

                    var dueToday = queue
                        .Where(h => h.DueDate.AddHours(6).Date == date.Date)
                        .ToList();

                    orderedQueue = urgentToday.Concat(dueToday).ToList();
                }

                foreach (var hw in orderedQueue)
                {
                    var duration = Math.Max(10, hw.EstimatedMinutes > 0 ? hw.EstimatedMinutes : 30);
                    if (usedMinutes + duration > totalAvailableMinutes)
                        break;

                    queue = new Queue<Homework>(queue.Where(h => h.Id != hw.Id));

                    var slotStart = studyStart.Add(TimeSpan.FromMinutes(usedMinutes));
                    var slotEnd   = slotStart.Add(TimeSpan.FromMinutes(duration));

                    slots.Add(new PlanningSlotDto(
                        Id: Guid.NewGuid(),
                        ChildId: hw.ChildId,
                        HomeworkId: hw.Id,
                        DayOfWeek: dayName,
                        StartTime: slotStart.ToString(@"hh\:mm"),
                        EndTime: slotEnd.ToString(@"hh\:mm"),
                        SlotType: "Study",
                        Title: hw.Title,
                        Subject: hw.Subject.ToString(),
                        Notes: hw.Description,
                        IsCompleted: hw.Status == HomeworkStatus.Completed,
                        IsAutoGenerated: true,
                        DurationMinutes: duration,
                        Order: order++
                    ));

                    usedMinutes += duration + BreakBetweenTasksMinutes;
                    totalMinutes += duration;
                }

                days.Add(new DayPlanDto(
                    DayOfWeek: dayName,
                    Slots: slots,
                    TotalMinutes: slots.Sum(s => s.DurationMinutes)
                ));
            }

            var completedMinutes = completed.Sum(h => h.ActualMinutes ?? h.EstimatedMinutes);

            return Result<WeeklyPlanningDto>.Success(new WeeklyPlanningDto(
                WeekNumber: request.WeekNumber,
                Year: request.Year,
                Days: days,
                TotalStudyMinutes: totalMinutes,
                CompletedMinutes: completedMinutes,
                ProgressPercentage: totalMinutes > 0
                    ? Math.Round((double)completedMinutes / (completedMinutes + totalMinutes) * 100, 1)
                    : 0
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Planning generation failed for child {ChildId}", request.ChildId);
            return Result<WeeklyPlanningDto>.Failure($"Planning generation failed: {ex.Message}", 500);
        }
    }

    public async Task<Result<WeeklyPlanningDto>> RebalancePlanAsync(
        Guid childId, int weekNumber, int year, CancellationToken ct = default)
    {
        return await GenerateWeeklyPlanAsync(
            new GeneratePlanningRequest(childId, weekNumber, year, null, AutoBalance: true), ct);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resolve the study time window for a given day. Falls back to default 16:00-18:30.
    /// </summary>
    private static (TimeSpan start, TimeSpan end) ResolveWindow(
        List<AvailabilitySlot>? slots, DayOfWeek dotNetDow)
    {
        if (slots is null || slots.Count == 0)
            return (DefaultStudyStart, DefaultStudyEnd);

        // Map .NET DayOfWeek → DayOfWeekCustom (both share Monday=1..Sunday=0/7)
        var customDow = dotNetDow == DayOfWeek.Sunday
            ? (DayOfWeekCustom)7
            : (DayOfWeekCustom)(int)dotNetDow;

        var matching = slots.Where(s => s.DayOfWeek == customDow).ToList();
        if (matching.Count == 0)
            return (DefaultStudyStart, DefaultStudyEnd);

        return (matching.Min(s => s.StartTime), matching.Max(s => s.EndTime));
    }
}
