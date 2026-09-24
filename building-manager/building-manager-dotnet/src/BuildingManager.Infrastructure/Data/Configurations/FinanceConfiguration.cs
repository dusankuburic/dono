using BuildingManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManager.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.BuildingId).IsRequired();
        builder.Property(p => p.UnitNumber).IsRequired().HasMaxLength(20);
        builder.Property(p => p.ResidentId);
        builder.Property(p => p.Amount).IsRequired().HasPrecision(12, 2);
        builder.Property(p => p.PaymentDate).IsRequired();
        builder.Property(p => p.Month);
        builder.Property(p => p.Year);
        builder.Property(p => p.Method).IsRequired();
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.Reference).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.RecordedBy).IsRequired();
        
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(p => p.BuildingId);
        builder.HasIndex(p => new { p.BuildingId, p.UnitNumber });
        builder.HasIndex(p => new { p.Year, p.Month });
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.BuildingId).IsRequired();
        builder.Property(e => e.Amount).IsRequired().HasPrecision(12, 2);
        builder.Property(e => e.ExpenseDate).IsRequired();
        builder.Property(e => e.Category).IsRequired();
        builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Vendor).HasMaxLength(200);
        builder.Property(e => e.InvoiceNumber).HasMaxLength(100);
        builder.Property(e => e.ReceiptUrl).HasMaxLength(500);
        builder.Property(e => e.WorkOrderId);
        builder.Property(e => e.RecordedBy).IsRequired();
        builder.Property(e => e.IsApproved).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.ApprovedBy);
        builder.Property(e => e.ApprovedAt);
        
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(e => e.BuildingId);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.ExpenseDate);
    }
}

public class FeeStructureConfiguration : IEntityTypeConfiguration<FeeStructure>
{
    public void Configure(EntityTypeBuilder<FeeStructure> builder)
    {
        builder.ToTable("FeeStructures");
        
        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.BuildingId).IsRequired();
        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
        builder.Property(f => f.AmountPerM2).IsRequired().HasPrecision(10, 4);
        builder.Property(f => f.FixedAmount).HasPrecision(12, 2);
        builder.Property(f => f.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(f => f.EffectiveFrom).IsRequired();
        builder.Property(f => f.EffectiveTo);
        builder.Property(f => f.Description).HasMaxLength(500);
        
        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.UpdatedAt).IsRequired();
        builder.Property(f => f.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasIndex(f => f.BuildingId);
        builder.HasIndex(f => new { f.BuildingId, f.IsActive });
    }
}
