using BuildingManager.Core.Entities;
using BuildingManager.Core.Interfaces;
using BuildingManager.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BuildingManager.Infrastructure.Data;

public class BuildingManagerDbContext : DbContext, IBuildingManagerDbContext
{
    public BuildingManagerDbContext(DbContextOptions<BuildingManagerDbContext> options) : base(options) { }

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<Key> Keys => Set<Key>();
    public DbSet<KeyTransaction> KeyTransactions => Set<KeyTransaction>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderNote> WorkOrderNotes => Set<WorkOrderNote>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Assembly> Assemblies => Set<Assembly>();
    public DbSet<AgendaItem> AgendaItems => Set<AgendaItem>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BuildingConfiguration());
        modelBuilder.ApplyConfiguration(new UnitConfiguration());
        modelBuilder.ApplyConfiguration(new ResidentConfiguration());
        modelBuilder.ApplyConfiguration(new KeyConfiguration());
        modelBuilder.ApplyConfiguration(new KeyTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new WorkOrderConfiguration());
        modelBuilder.ApplyConfiguration(new WorkOrderNoteConfiguration());
        modelBuilder.ApplyConfiguration(new AnnouncementConfiguration());
        modelBuilder.ApplyConfiguration(new AssemblyConfiguration());
        modelBuilder.ApplyConfiguration(new AgendaItemConfiguration());
        modelBuilder.ApplyConfiguration(new VoteConfiguration());
        modelBuilder.ApplyConfiguration(new AttendanceConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
        modelBuilder.ApplyConfiguration(new FeeStructureConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(BaseEntity.CreatedAt)).CurrentValue = DateTime.UtcNow;
                    entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
                    break;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}
