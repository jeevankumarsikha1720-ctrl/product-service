using ProductService.Domain.Entities;
using ProductService.Domain.Inventory;

namespace ProductService.Application.Products.Dtos;

/// <summary>
/// Read model for a Product, enriched with live inventory state when available.
///
/// • StockQuantity is kept for backward compatibility but now reflects Available
///   (OnHand - Reserved) sourced from InventoryItem - NOT the legacy Product.StockQuantity
///   column, which is no longer authoritative.
/// • OnHand / Reserved / IsLowStock are surfaced so admin views can show the split.
/// • If a Product somehow has no InventoryItem (shouldn't happen post-backfill, but
///   defensive), all inventory fields default to 0/false.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,        // = Available, sourced from InventoryItem
    int OnHand,
    int Reserved,
    bool IsLowStock,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    /// <summary>
    /// Build a DTO from a Product alone. Used when inventory hasn't been loaded
    /// or doesn't exist yet (e.g. immediately after Product creation, before save).
    /// All inventory fields fall back to the legacy Product.StockQuantity value.
    /// </summary>
    public static ProductDto FromEntity(Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.Currency,
        p.StockQuantity,           // StockQuantity (Available)
        p.StockQuantity,           // OnHand (best guess from legacy column)
        0,                         // Reserved
        false,                     // IsLowStock
        p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc);

    /// <summary>
    /// Build a DTO from a Product + its live InventoryItem. This is the preferred
    /// path for any read endpoint - the frontend gets the truth, not a stale cache.
    /// </summary>
    public static ProductDto FromEntities(Product p, InventoryItem? inventory)
    {
        if (inventory is null) return FromEntity(p);

        return new ProductDto(
            p.Id, p.Name, p.Description, p.Price, p.Currency,
            inventory.Available,            // StockQuantity = Available (the only number a customer cares about)
            inventory.OnHand,
            inventory.Reserved,
            inventory.IsLowStock,
            p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc);
    }
}
