using BuildingManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManager.Infrastructure.Data.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.BuildingId).IsRequired();
        builder.Property(u => u.Number).IsRequired().HasMaxLength(20);
        builder.Property(u => u.Floor).IsRequired();
        builder.Property(u => u.Entrance).HasMaxLength(10);
        builder.Property(u => u.AreaM2).IsRequired().HasPrecision(10, 2);
        builder.Property(u => u.OwnershipShare).IsRequired().HasPrecision(8, 4);
        builder.Property(u => u.Type).IsRequired();
        builder.Property(u => u.Status).IsRequired();
        
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(u => new { u.BuildingId, u.Number }).IsUnique();
        builder.HasIndex(u => u.BuildingId);
    }
}

public class ResidentConfiguration : IEntityTypeConfiguration<Resident>
{
    public void Configure(EntityTypeBuilder<Resident> builder)
    {
        builder.ToTable("Residents");
        
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.BuildingId).IsRequired();
        builder.Property(r => r.UnitId);
        builder.Property(r => r.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.LastName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Phone).HasMaxLength(30);
        builder.Property(r => r.Email).HasMaxLength(200);
        builder.Property(r => r.Role).IsRequired();
        builder.Property(r => r.OwnershipShare).HasPrecision(8, 4);
        builder.Property(r => r.EmergencyContactName).HasMaxLength(200);
        builder.Property(r => r.EmergencyContactPhone).HasMaxLength(30);
        builder.Property(r => r.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(r => r.MoveInDate);
        builder.Property(r => r.MoveOutDate);
        builder.Property(r => r.MissedAssembliesCount).IsRequired().HasDefaultValue(0);
        
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(r => r.BuildingId);
        builder.HasIndex(r => r.UnitId);
        builder.HasIndex(r => new { r.FirstName, r.LastName });
    }
}

public class KeyConfiguration : IEntityTypeConfiguration<Key>
{
    public void Configure(EntityTypeBuilder<Key> builder)
    {
        builder.ToTable("Keys");
        
        builder.HasKey(k => k.Id);
        
        builder.Property(k => k.BuildingId).IsRequired();
        builder.Property(k => k.Type).IsRequired();
        builder.Property(k => k.UnitNumber).HasMaxLength(20);
        builder.Property(k => k.Identifier).IsRequired().HasMaxLength(100);
        builder.Property(k => k.Status).IsRequired();
        builder.Property(k => k.Description).HasMaxLength(500);
        
        builder.Property(k => k.CreatedAt).IsRequired();
        builder.Property(k => k.UpdatedAt).IsRequired();
        builder.Property(k => k.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasMany(k => k.Transactions)
            .WithOne()
            .HasForeignKey(t => t.KeyId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(k => k.BuildingId);
        builder.HasIndex(k => k.Status);
    }
}

public class KeyTransactionConfiguration : IEntityTypeConfiguration<KeyTransaction>
{
    public void Configure(EntityTypeBuilder<KeyTransaction> builder)
    {
        builder.ToTable("KeyTransactions");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.KeyId).IsRequired();
        builder.Property(t => t.RecipientName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.RecipientPhone).HasMaxLength(30);
        builder.Property(t => t.RecipientResidentId);
        builder.Property(t => t.RecipientType).IsRequired();
        builder.Property(t => t.Purpose).IsRequired().HasMaxLength(500);
        builder.Property(t => t.CheckedOutAt).IsRequired();
        builder.Property(t => t.ExpectedReturn).IsRequired();
        builder.Property(t => t.ReturnedAt);
        builder.Property(t => t.ReturnCondition).HasMaxLength(100);
        builder.Property(t => t.Notes).HasMaxLength(500);
        
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
        builder.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(t => t.KeyId);
        builder.HasIndex(t => t.ReturnedAt);
    }
}
