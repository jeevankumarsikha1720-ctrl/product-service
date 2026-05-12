using ProductService.Domain.Entities;

namespace ProductService.Application.Interfaces;

/// <summary>
/// Persistence contract for the Product aggregate. Lives in Application
/// so handlers depend on the abstraction, not on EF Core.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Remove(Product product);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
