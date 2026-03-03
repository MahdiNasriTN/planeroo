using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Domain.Entities;

/// <summary>
/// OCR scan session of a school agenda.
/// </summary>
public class ScanSession : BaseEntity
{
    public Guid ChildId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? RawOcrText { get; set; }
    public string? ProcessedText { get; set; }
    public ScanStatus Status { get; set; } = ScanStatus.Processing;
    public int DetectedTasksCount { get; set; } = 0;
    public int ConfirmedTasksCount { get; set; } = 0;
    public double? ConfidenceScore { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public Child Child { get; set; } = null!;
    public ICollection<Homework> DetectedHomeworks { get; set; } = new List<Homework>();
}
