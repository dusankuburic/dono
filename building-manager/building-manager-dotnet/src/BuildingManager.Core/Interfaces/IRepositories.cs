using System.Linq.Expressions;
using BuildingManager.Core.Entities;

namespace BuildingManager.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    IBuildingRepository Buildings { get; }
    IUnitRepository Units { get; }
    IResidentRepository Residents { get; }
    IKeyRepository Keys { get; }
    IWorkOrderRepository WorkOrders { get; }
    IAssemblyRepository Assemblies { get; }
    IPaymentRepository Payments { get; }
    IExpenseRepository Expenses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IBuildingRepository : IRepository<Building>
{
    Task<Building?> GetWithUnitsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Building?> GetWithResidentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Building?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUnitRepository : IRepository<Unit>
{
    Task<IEnumerable<Unit>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
}

public interface IResidentRepository : IRepository<Resident>
{
    Task<IEnumerable<Resident>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Resident>> GetByUnitIdAsync(Guid unitId, CancellationToken cancellationToken = default);
}

public interface IKeyRepository : IRepository<Key>
{
    Task<IEnumerable<Key>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Key>> GetOverdueKeysAsync(Guid buildingId, CancellationToken cancellationToken = default);
}

public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    Task<IEnumerable<WorkOrder>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkOrder>> GetOpenWorkOrdersAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkOrder>> GetEmergencyWorkOrdersAsync(Guid buildingId, CancellationToken cancellationToken = default);
}

public interface IAssemblyRepository : IRepository<Assembly>
{
    Task<IEnumerable<Assembly>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<Assembly?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByUnitNumberAsync(Guid buildingId, string unitNumber, CancellationToken cancellationToken = default);
}

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IEnumerable<Expense>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
