using BuildingManager.Core.Entities;
using BuildingManager.Core.Enums;
using BuildingManager.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManager.Infrastructure.Data.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("Buildings");
        
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.OwnsOne(b => b.Address, address =>
        {
            address.Property(a => a.Street).IsRequired().HasMaxLength(200);
            address.Property(a => a.StreetNumber).IsRequired().HasMaxLength(20);
            address.Property(a => a.ApartmentNumber).HasMaxLength(20);
            address.Property(a => a.City).IsRequired().HasMaxLength(100);
            address.Property(a => a.PostalCode).HasMaxLength(20);
            address.Property(a => a.Country).HasMaxLength(100).HasDefaultValue("Serbia");
        });
        
        builder.Property(b => b.YearBuilt);
        builder.Property(b => b.TotalFloors).IsRequired();
        builder.Property(b => b.HasElevator).IsRequired().HasDefaultValue(false);
        builder.Property(b => b.HasParking).IsRequired().HasDefaultValue(false);
        builder.Property(b => b.TotalAreaM2).IsRequired().HasPrecision(10, 2);
        builder.Property(b => b.Status).IsRequired();
        
        builder.Property(b => b.IsLegalEntity).IsRequired().HasDefaultValue(false);
        builder.Property(b => b.LegalEntityName).HasMaxLength(200);
        builder.Property(b => b.MbNumber).HasMaxLength(20);
        builder.Property(b => b.Pib).HasMaxLength(20);
        builder.Property(b => b.BankAccount).HasMaxLength(50);
        builder.Property(b => b.RegistrationDate);
        
        builder.Property(b => b.ManagerType).IsRequired().HasDefaultValue(ManagerType.Regular);
        builder.Property(b => b.ManagerId);
        builder.Property(b => b.ManagerLicenseNumber).HasMaxLength(50);
        builder.Property(b => b.ManagerAssignedDate);
        
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();
        builder.Property(b => b.IsDeleted).IsRequired().HasDefaultValue(false);
        
        builder.HasMany(b => b.Units)
            .WithOne()
            .HasForeignKey(u => u.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(b => b.Residents)
            .WithOne()
            .HasForeignKey(r => r.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(b => b.Name);
        builder.HasIndex(b => b.IsLegalEntity);
    }
}
