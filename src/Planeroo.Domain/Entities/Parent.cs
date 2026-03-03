using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Domain.Entities;

/// <summary>
/// Parent user account - primary account holder.
/// </summary>
public class Parent : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string Timezone { get; set; } = "Europe/Paris";
    public string Language { get; set; } = "fr";
    public bool IsEmailVerified { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Privacy & GDPR
    public bool HasAcceptedTerms { get; set; } = false;
    public DateTime? TermsAcceptedAt { get; set; }
    public bool DataProcessingConsent { get; set; } = false;

    // Notification preferences
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyByPush { get; set; } = true;
    public bool WeeklyReportEnabled { get; set; } = true;

    // Navigation
    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
