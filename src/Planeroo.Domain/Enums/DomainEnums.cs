using System.Text.Json.Serialization;

namespace Planeroo.Domain.Enums;

public enum UserRole
{
    Parent = 0,
    Child = 1
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HomeworkStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Overdue = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HomeworkPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubjectType
{
    Mathematics = 0,
    French = 1,
    English = 2,
    Science = 3,
    History = 4,
    Geography = 5,
    Art = 6,
    Music = 7,
    PhysicalEducation = 8,
    Technology = 9,
    Other = 99
}

public enum BadgeCategory
{
    Streak = 0,
    Completion = 1,
    Speed = 2,
    Consistency = 3,
    Explorer = 4,
    Helper = 5,
    Champion = 6
}

public enum NotificationType
{
    HomeworkReminder = 0,
    AchievementUnlocked = 1,
    WeeklyReport = 2,
    PlanningUpdate = 3,
    ParentAlert = 4,
    StreakWarning = 5,
    NewBadge = 6
}

public enum ScanStatus
{
    Processing = 0,
    Completed = 1,
    Failed = 2,
    RequiresReview = 3
}

public enum PlanningSlotType
{
    Study = 0,
    Break = 1,
    Review = 2,
    Practice = 3
}

public enum DayOfWeekCustom
{
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6,
    Sunday = 7
}
