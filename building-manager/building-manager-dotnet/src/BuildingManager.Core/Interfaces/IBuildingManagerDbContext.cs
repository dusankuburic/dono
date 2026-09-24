namespace BuildingManager.Core.Interfaces;

public interface IBuildingManagerDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
