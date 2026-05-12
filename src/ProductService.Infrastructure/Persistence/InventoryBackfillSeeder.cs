using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductService.Domain.Inventory;

namespace ProductService.Infrastructure.Persistence;

/// <summary>
/// Idempotent: creates an InventoryItem for every Product that doesn't have one yet,
/// using Product.StockQuantity as the initial OnHand.
///
/// Run once at startup after migrations apply. Safe to run repeatedly - it's a
/// LEFT JOIN check, no duplicate inserts.
/// </summary>
public static class InventoryBackfillSeeder
{
    public static async Task RunAsync(ProductDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var productsWithoutInventory =
            from product in db.Products.AsNoTracking()
            join inv in db.InventoryItems.AsNoTracking()
                on product.Id equals inv.ProductId into joined
            from inv in joined.DefaultIfEmpty()
            where inv == null
            select new { product.Id, product.StockQuantity };

        var toBackfill = await productsWithoutInventory.ToListAsync(ct);
        if (toBackfill.Count == 0) return;

        logger.LogInformation("Backfilling InventoryItems for {Count} products without one.", toBackfill.Count);

        foreach (var row in toBackfill)
        {
            var item = InventoryItem.Create(row.Id, row.StockQuantity, lowStockThreshold: 0);
            await db.InventoryItems.AddAsync(item, ct);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Backfill complete.");
    }
}
