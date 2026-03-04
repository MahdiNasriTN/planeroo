using Microsoft.EntityFrameworkCore;
using Planeroo.Domain.Entities;

namespace Planeroo.Infrastructure.Persistence;

/// <summary>
/// Main database context for Planeroo.
/// </summary>
public class PlanerooDbContext : DbContext
{
    public PlanerooDbContext(DbContextOptions<PlanerooDbContext> options) : base(options) { }

    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Homework> Homeworks => Set<Homework>();
    public DbSet<PlanningSlot> PlanningSlots => Set<PlanningSlot>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AIInteraction> AIInteractions => Set<AIInteraction>();
    public DbSet<ScanSession> ScanSessions => Set<ScanSession>();
    public DbSet<StudySheet> StudySheets => Set<StudySheet>();
    public DbSet<ChildTimetable> ChildTimetables => Set<ChildTimetable>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Parent ──
        modelBuilder.Entity<Parent>(e =>
        {
            e.ToTable("parents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.Timezone).HasMaxLength(50).HasDefaultValue("Europe/Paris");
            e.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("fr");
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
            e.Property(x => x.RefreshToken).HasMaxLength(512);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Child ──
        modelBuilder.Entity<Child>(e =>
        {
            e.ToTable("children");
            e.HasKey(x => x.Id);
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.SchoolName).HasMaxLength(200);
            e.Property(x => x.Pin).HasMaxLength(10);
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
            e.Property(x => x.FavoriteColor).HasMaxLength(20);
            e.Property(x => x.MascotName).HasMaxLength(50).HasDefaultValue("Roo");
            e.Ignore(x => x.Age);

            e.HasOne(x => x.Parent)
             .WithMany(p => p.Children)
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Homework ──
        modelBuilder.Entity<Homework>(e =>
        {
            e.ToTable("homeworks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.Subject).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(30);

            e.HasOne(x => x.Child)
             .WithMany(c => c.Homeworks)
             .HasForeignKey(x => x.ChildId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ScanSession)
             .WithMany(s => s.DetectedHomeworks)
             .HasForeignKey(x => x.ScanSessionId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.ChildId, x.DueDate });
            e.HasIndex(x => new { x.ChildId, x.Status });
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Planning Slot ──
        modelBuilder.Entity<PlanningSlot>(e =>
        {
            e.ToTable("planning_slots");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.SlotType).HasConversion<string>().HasMaxLength(20);
            e.Ignore(x => x.DurationMinutes);

            e.HasOne(x => x.Child)
             .WithMany(c => c.PlanningSlots)
             .HasForeignKey(x => x.ChildId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Homework)
             .WithMany(h => h.PlanningSlots)
             .HasForeignKey(x => x.HomeworkId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.ChildId, x.WeekNumber, x.Year });
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Badge ──
        modelBuilder.Entity<Badge>(e =>
        {
            e.ToTable("badges");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(300).IsRequired();
            e.Property(x => x.IconName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);

            e.HasOne(x => x.Child)
             .WithMany(c => c.Badges)
             .HasForeignKey(x => x.ChildId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Notification ──
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ActionUrl).HasMaxLength(500);

            e.HasOne(x => x.Parent)
             .WithMany(p => p.Notifications)
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ParentId, x.IsRead });
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── AI Interaction ──
        modelBuilder.Entity<AIInteraction>(e =>
        {
            e.ToTable("ai_interactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserMessage).HasMaxLength(2000).IsRequired();
            e.Property(x => x.AIResponse).HasMaxLength(5000).IsRequired();
            e.Property(x => x.Topic).HasMaxLength(100);
            e.Property(x => x.FilterReason).HasMaxLength(500);

            e.HasOne(x => x.Child)
             .WithMany(c => c.AIInteractions)
             .HasForeignKey(x => x.ChildId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.ChildId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Scan Session ──
        modelBuilder.Entity<ScanSession>(e =>
        {
            e.ToTable("scan_sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            e.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            e.Property(x => x.RawOcrText).HasMaxLength(10000);
            e.Property(x => x.ProcessedText).HasMaxLength(10000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ErrorMessage).HasMaxLength(1000);

            e.HasOne(x => x.Child)
             .WithMany(c => c.ScanSessions)
             .HasForeignKey(x => x.ChildId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Study Sheet ──
        modelBuilder.Entity<StudySheet>(e =>
        {
            e.ToTable("study_sheets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Subject).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Content).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(1000);

            e.HasOne(x => x.Child)
             .WithMany(c => c.StudySheets)
             .HasForeignKey(x => x.ChildId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Child Timetable ──
        modelBuilder.Entity<ChildTimetable>(e =>
        {
            e.ToTable("child_timetables");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ChildId).IsUnique();

            e.HasOne(x => x.Child)
             .WithOne(c => c.Timetable)
             .HasForeignKey<ChildTimetable>(x => x.ChildId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Timetable Entry ──
        modelBuilder.Entity<TimetableEntry>(e =>
        {
            e.ToTable("timetable_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.DayOfWeek).HasMaxLength(20).IsRequired();
            e.Property(x => x.StartTime).HasMaxLength(10).IsRequired();
            e.Property(x => x.EndTime).HasMaxLength(10).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(100).IsRequired();
            e.Property(x => x.SubjectDisplayName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Period).HasMaxLength(20);

            e.HasOne(x => x.Timetable)
             .WithMany(t => t.Entries)
             .HasForeignKey(x => x.TimetableId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(ct);
    }
}
