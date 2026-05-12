using ProductService.Domain.Inventory;

namespace ProductService.Application.Inventory.Dtos;

public sealed record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    int OnHand,
    int Reserved,
    int Available,
    int LowStockThreshold,
    bool IsLowStock,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static InventoryItemDto FromEntity(InventoryItem item) => new(
        item.Id, item.ProductId, item.OnHand, item.Reserved, item.Available,
        item.LowStockThreshold, item.IsLowStock,
        item.CreatedAtUtc, item.UpdatedAtUtc);
}
