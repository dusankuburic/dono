using BuildingManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildingManager.Infrastructure;

/// <summary>
/// Lets `dotnet ef` tooling create migrations for BuildingManagerDbContext without
/// a runnable host project. The connection string only matters for scaffolding;
/// migrations are detected via the model, not the live database.
/// </summary>
public class BuildingManagerDbContextFactory : IDesignTimeDbContextFactory<BuildingManagerDbContext>
{
    public BuildingManagerDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.Length > 0
            ? args[0]
            : "Host=localhost;Port=5432;Database=building_manager;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<BuildingManagerDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BuildingManagerDbContext(options);
    }
}
