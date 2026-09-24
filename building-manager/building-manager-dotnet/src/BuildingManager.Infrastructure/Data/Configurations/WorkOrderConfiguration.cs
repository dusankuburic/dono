using BuildingManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManager.Infrastructure.Data.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");
        
        builder.HasKey(w => w.Id);
        
        builder.Property(w => w.BuildingId).IsRequired();
        builder.Property(w => w.ReferenceNumber).IsRequired().HasMaxLength(50);
        builder.Property(w => w.Title).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Description).IsRequired().HasMaxLength(2000);
        builder.Property(w => w.Category).IsRequired();
        builder.Property(w => w.Priority).IsRequired();
        builder.Property(w => w.Status).IsRequired();
        builder.Property(w => w.UnitNumber).HasMaxLength(20);
        builder.Property(w => w.ReportedByResidentId).IsRequired();
        builder.Property(w => w.AssignedToId);
        builder.Property(w => w.IsEmergency).IsRequired().HasDefaultValue(false);
        builder.Property(w => w.EmergencyStartedAt);
        builder.Property(w => w.EmergencyDeadline);
        builder.Property(w => w.EstimatedCost).HasPrecision(12, 2);
        builder.Property(w => w.ActualCost).HasPrecision(12, 2);
        builder.Property(w => w.CompletedAt);
        builder.Property(w => w.TargetCompletionDate);
        
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt).IsRequired();
        builder.Property(w => w.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasMany(w => w.Notes)
            .WithOne()
            .HasForeignKey(n => n.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(w => w.BuildingId);
        builder.HasIndex(w => w.Status);
        builder.HasIndex(w => w.ReferenceNumber).IsUnique();
    }
}

public class WorkOrderNoteConfiguration : IEntityTypeConfiguration<WorkOrderNote>
{
    public void Configure(EntityTypeBuilder<WorkOrderNote> builder)
    {
        builder.ToTable("WorkOrderNotes");
        
        builder.HasKey(n => n.Id);
        
        builder.Property(n => n.WorkOrderId).IsRequired();
        builder.Property(n => n.CreatedBy).IsRequired();
        builder.Property(n => n.Content).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.IsInternal).IsRequired().HasDefaultValue(false);
        
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.UpdatedAt).IsRequired();
        builder.Property(n => n.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(n => n.WorkOrderId);
    }
}

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.BuildingId).IsRequired();
        builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Content).IsRequired().HasMaxLength(5000);
        builder.Property(a => a.Type).IsRequired();
        builder.Property(a => a.ScheduledFor);
        builder.Property(a => a.SentAt);
        builder.Property(a => a.CreatedBy).IsRequired();
        builder.Property(a => a.IsPinned).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.ExpiresAt);
        
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(a => a.BuildingId);
        builder.HasIndex(a => a.SentAt);
    }
}

public class AssemblyConfiguration : IEntityTypeConfiguration<Assembly>
{
    public void Configure(EntityTypeBuilder<Assembly> builder)
    {
        builder.ToTable("Assemblies");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.BuildingId).IsRequired();
        builder.Property(a => a.Date).IsRequired();
        builder.Property(a => a.Location).IsRequired().HasMaxLength(200);
        builder.Property(a => a.SessionType).IsRequired();
        builder.Property(a => a.Status).IsRequired();
        builder.Property(a => a.QuorumAchieved).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.QuorumPercentage).HasPrecision(6, 2);
        builder.Property(a => a.NoticeSentAt);
        builder.Property(a => a.StartedAt);
        builder.Property(a => a.CompletedAt);
        
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasMany(a => a.AgendaItems)
            .WithOne()
            .HasForeignKey(ai => ai.AssemblyId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(a => a.Attendances)
            .WithOne()
            .HasForeignKey(att => att.AssemblyId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(a => a.BuildingId);
        builder.HasIndex(a => a.Date);
    }
}

public class AgendaItemConfiguration : IEntityTypeConfiguration<AgendaItem>
{
    public void Configure(EntityTypeBuilder<AgendaItem> builder)
    {
        builder.ToTable("AgendaItems");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.AssemblyId).IsRequired();
        builder.Property(a => a.Order).IsRequired();
        builder.Property(a => a.Title).IsRequired().HasMaxLength(300);
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.RequiresVote).IsRequired().HasDefaultValue(true);
        builder.Property(a => a.VotingType).IsRequired();
        builder.Property(a => a.Result).IsRequired();
        builder.Property(a => a.VotesFor).HasPrecision(10, 4);
        builder.Property(a => a.VotesAgainst).HasPrecision(10, 4);
        builder.Property(a => a.VotesAbstain).HasPrecision(10, 4);
        builder.Property(a => a.IsFinalized).IsRequired().HasDefaultValue(false);
        
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasMany(a => a.Votes)
            .WithOne()
            .HasForeignKey(v => v.AgendaItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(a => a.AssemblyId);
    }
}

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("Votes");
        
        builder.HasKey(v => v.Id);
        
        builder.Property(v => v.AgendaItemId).IsRequired();
        builder.Property(v => v.ResidentId).IsRequired();
        builder.Property(v => v.Choice).IsRequired();
        builder.Property(v => v.VotingShare).HasPrecision(10, 4);
        
        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt).IsRequired();
        builder.Property(v => v.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(v => v.AgendaItemId);
        builder.HasIndex(v => new { v.AgendaItemId, v.ResidentId }).IsUnique();
    }
}

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.AssemblyId).IsRequired();
        builder.Property(a => a.ResidentId).IsRequired();
        builder.Property(a => a.Type).IsRequired();
        builder.Property(a => a.VotingShare).HasPrecision(10, 4);
        builder.Property(a => a.ProxyHolderId);
        builder.Property(a => a.AttendedAt);
        builder.Property(a => a.DidAttend).IsRequired().HasDefaultValue(false);
        
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(a => a.AssemblyId);
        builder.HasIndex(a => new { a.AssemblyId, a.ResidentId }).IsUnique();
    }
}
