using ProductService.Application.Common.Models;
using ProductService.Domain.Inventory;

namespace ProductService.Application.Interfaces;

/// <summary>
/// Persistence contract for the Inventory aggregate.
/// All mutations go through the aggregate's methods - the repository just
/// loads, tracks, and saves.
/// </summary>
public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid inventoryItemId, CancellationToken ct = default);

    /// <summary>Looks up the inventory record for a given product. Returns null if uninitialised.</summary>
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Batch load inventory for many products in one query. Returns a dictionary
    /// keyed by ProductId. Used by the products-list endpoint to avoid an N+1.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, InventoryItem>> GetByProductIdsAsync(
        IEnumerable<Guid> productIds, CancellationToken ct = default);

    /// <summary>Items below their LowStockThreshold, ordered by most-urgent first.</summary>
    Task<IReadOnlyList<InventoryItem>> ListLowStockAsync(CancellationToken ct = default);

    /// <summary>Paged movement history for one inventory item, newest first.</summary>
    Task<PagedResult<InventoryMovement>> ListMovementsAsync(
        Guid inventoryItemId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(InventoryItem item, CancellationToken ct = default);

    void Remove(InventoryItem item);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
