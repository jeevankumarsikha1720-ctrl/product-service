using ProductService.Application.Common.Exceptions;
using ProductService.Application.Interfaces;
using ProductService.Domain.Inventory;

namespace ProductService.Application.Inventory;

/// <summary>
/// Small helper to keep "load or throw 404" out of every command handler.
/// </summary>
internal static class InventoryLookupExtensions
{
    public static async Task<InventoryItem> LoadByProductOrThrowAsync(
       this IInventoryRepository repo,
       Guid productId,
       CancellationToken ct = default)
    {
        return await repo.GetByProductIdAsync(productId, ct)
            ?? throw new InvalidOperationException("Inventory item was not found for this product.");
    }

    public static async Task<InventoryItem> LoadByIdOrThrowAsync(
        this IInventoryRepository repo, Guid inventoryItemId, CancellationToken ct)
    {
        return await repo.GetByIdAsync(inventoryItemId, ct)
            ?? throw new NotFoundException("InventoryItem", inventoryItemId);
    }
}
