using Microsoft.EntityFrameworkCore;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure.Repositories;

public sealed class ProductRepository(ProductDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // SQL Server LIKE is case-insensitive by default (depends on collation).
            var term = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Name, term));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
        => await db.Products.AddAsync(product, ct);

    public void Remove(Product product) => db.Products.Remove(product);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
