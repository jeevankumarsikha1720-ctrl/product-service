using Microsoft.EntityFrameworkCore;
using ProductService.Application.Common.Models;
using ProductService.Application.Interfaces;
using ProductService.Domain.Inventory;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure.Repositories;

public sealed class InventoryRepository(ProductDbContext db) : IInventoryRepository
{
    public async Task<InventoryItem?> GetByIdAsync(
    Guid inventoryItemId,
    CancellationToken ct = default)
    {
        return await db.InventoryItems
            .FirstOrDefaultAsync(x => x.Id == inventoryItemId, ct);
    }

    public async Task<InventoryItem?> GetByProductIdAsync(
     Guid productId,
     CancellationToken ct = default)
    {
        return await db.InventoryItems
            .FirstOrDefaultAsync(x => x.ProductId == productId, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, InventoryItem>> GetByProductIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken ct = default)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, InventoryItem>();

        // AsNoTracking - this path is read-only; mutations always go through
        // GetByProductIdAsync which DOES track for change-saving.
        var items = await db.InventoryItems
            .AsNoTracking()
            .Where(x => ids.Contains(x.ProductId))
            .ToListAsync(ct);

        return items.ToDictionary(x => x.ProductId);
    }

    public async Task<IReadOnlyList<InventoryItem>> ListLowStockAsync(
        CancellationToken ct = default)
    {
        // OrderBy(x => x.Available) would throw at runtime because Available
        // is a computed property marked Ignore in EF. Use the underlying
        // expression so EF can translate it to SQL.
        return await db.InventoryItems
            .AsNoTracking()
            .Where(x => x.OnHand <= x.LowStockThreshold)
            .OrderBy(x => x.OnHand - x.Reserved)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<InventoryMovement>> ListMovementsAsync(
        Guid inventoryItemId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.InventoryMovements
            .AsNoTracking()
            .Where(x => x.InventoryItemId == inventoryItemId)
            .OrderByDescending(x => x.OccurredAtUtc);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<InventoryMovement>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task AddAsync(
        InventoryItem item,
        CancellationToken ct = default)
    {
        await db.InventoryItems.AddAsync(item, ct);
    }

    public void Remove(InventoryItem item)
    {
        db.InventoryItems.Remove(item);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
