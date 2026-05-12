using ProductService.Domain.Common;
using ProductService.Domain.Exceptions;

namespace ProductService.Domain.Inventory;

/// <summary>
/// Inventory aggregate root. One row per Product (1:1 for now - this could
/// become 1:N when we add warehouse locations).
///
/// Owns the invariants:
///   • OnHand >= 0 always
///   • Reserved >= 0 always
///   • Reserved <= OnHand always   (can't reserve what we don't have)
///   • Available = OnHand - Reserved (computed)
///
/// All state changes go through methods, never setters. Every method that
/// changes state appends an InventoryMovement, so the full audit log is
/// produced automatically.
/// </summary>
public sealed class InventoryItem : BaseEntity
{
    public Guid ProductId { get; private set; }

    /// <summary>Physical units in the warehouse.</summary>
    public int OnHand { get; private set; }

    /// <summary>Units held against active carts/orders. Not yet shipped.</summary>
    public int Reserved { get; private set; }

    /// <summary>Computed: what's actually buyable right now.</summary>
    public int Available => OnHand - Reserved;

    /// <summary>Below this OnHand value, this item shows up in the low-stock report.</summary>
    public int LowStockThreshold { get; private set; }

    public bool IsLowStock => OnHand <= LowStockThreshold;

    private readonly List<InventoryMovement> _movements = new();
    public IReadOnlyCollection<InventoryMovement> Movements => _movements.AsReadOnly();

    // EF Core
    private InventoryItem() { }

    public static InventoryItem Create(Guid productId, int initialOnHand, int lowStockThreshold = 0)
    {
        if (productId == Guid.Empty)
            throw new DomainException("ProductId is required.");
        if (initialOnHand < 0)
            throw new DomainException("Initial OnHand cannot be negative.");
        if (lowStockThreshold < 0)
            throw new DomainException("LowStockThreshold cannot be negative.");

        var item = new InventoryItem
        {
            ProductId = productId,
            OnHand = initialOnHand,
            Reserved = 0,
            LowStockThreshold = lowStockThreshold,
        };

        if (initialOnHand > 0)
        {
            item._movements.Add(new InventoryMovement(
                item.Id, initialOnHand, +initialOnHand, 0,
                InventoryMovementReason.Received, null, "Initial stock"));
        }

        return item;
    }

    // ───────── Inventory operations ─────────

    /// <summary>Stock physically arrived. Increases OnHand.</summary>
    public void Receive(int qty, Guid? referenceId = null, string? note = null)
    {
        RequirePositive(qty);
        OnHand += qty;
        Record(qty, +qty, 0, InventoryMovementReason.Received, referenceId, note);
        Touch();
    }

    /// <summary>Hold units for an active cart or pending order. Throws if not enough is Available.</summary>
    public void Reserve(int qty, Guid referenceId, string? note = null)
    {
        RequirePositive(qty);
        if (qty > Available)
            throw new DomainException($"Cannot reserve {qty}; only {Available} available (OnHand={OnHand}, Reserved={Reserved}).");

        Reserved += qty;
        Record(qty, 0, +qty, InventoryMovementReason.Reserved, referenceId, note);
        Touch();
    }

    /// <summary>Release a prior reservation (cart abandoned, order cancelled before fulfillment).</summary>
    public void Release(int qty, Guid referenceId, string? note = null)
    {
        RequirePositive(qty);
        if (qty > Reserved)
            throw new DomainException($"Cannot release {qty}; only {Reserved} reserved.");

        Reserved -= qty;
        Record(qty, 0, -qty, InventoryMovementReason.Released, referenceId, note);
        Touch();
    }

    /// <summary>Order fulfilled: stock ships out. Decreases both OnHand and Reserved together.</summary>
    public void Commit(int qty, Guid referenceId, string? note = null)
    {
        RequirePositive(qty);
        if (qty > Reserved)
            throw new DomainException($"Cannot commit {qty}; only {Reserved} reserved.");

        OnHand -= qty;
        Reserved -= qty;
        Record(qty, -qty, -qty, InventoryMovementReason.Sold, referenceId, note);
        Touch();
    }

    /// <summary>Customer returned a delivered item in resellable condition. OnHand goes back up.</summary>
    public void ReturnFromSale(int qty, Guid referenceId, string? note = null)
    {
        RequirePositive(qty);
        OnHand += qty;
        Record(qty, +qty, 0, InventoryMovementReason.Returned, referenceId, note);
        Touch();
    }

    /// <summary>
    /// Chargeback received and the item physically came back. Same effect as a normal return,
    /// but logged with a chargeback-specific reason for audit/reporting.
    /// </summary>
    public void ReturnFromChargeback(int qty, Guid referenceId, string? note = null)
    {
        RequirePositive(qty);
        OnHand += qty;
        Record(qty, +qty, 0, InventoryMovementReason.ReturnedFromChargeback, referenceId, note);
        Touch();
    }

    /// <summary>
    /// Chargeback received but the item never came back (friendly fraud / lost in transit).
    /// OnHand is already gone - this just records the loss for reporting.
    /// </summary>
    public void WriteOffFromChargeback(int qty, Guid referenceId, string? note = null)
    {
        RequirePositive(qty);
        // OnHand was already decremented when we Committed the sale.
        // This movement is informational - tags the loss so accounting can reconcile.
        Record(qty, 0, 0, InventoryMovementReason.ChargebackLoss, referenceId, note);
        Touch();
    }

    /// <summary>Stock damaged, expired, or otherwise unsellable. Decreases OnHand.</summary>
    public void WriteOff(int qty, string? note = null)
    {
        RequirePositive(qty);
        if (qty > Available)
            throw new DomainException($"Cannot write off {qty}; only {Available} available (Reserved units cannot be written off without releasing first).");

        OnHand -= qty;
        Record(qty, -qty, 0, InventoryMovementReason.WriteOff, null, note);
        Touch();
    }

    /// <summary>Manual positive adjustment (recount surplus, data fix).</summary>
    public void AdjustIn(int qty, string? note = null)
    {
        RequirePositive(qty);
        OnHand += qty;
        Record(qty, +qty, 0, InventoryMovementReason.ManualAdjustmentIn, null, note);
        Touch();
    }

    /// <summary>Manual negative adjustment (recount shortage, theft, data fix).</summary>
    public void AdjustOut(int qty, string? note = null)
    {
        RequirePositive(qty);
        if (qty > Available)
            throw new DomainException($"Cannot adjust out {qty}; only {Available} available.");

        OnHand -= qty;
        Record(qty, -qty, 0, InventoryMovementReason.ManualAdjustmentOut, null, note);
        Touch();
    }

    /// <summary>
    /// Physical recount: replace OnHand with the new absolute value.
    /// Records the delta (signed) in the movement row.
    /// Reserved is untouched - if recount reveals OnHand &lt; Reserved, throws.
    /// </summary>
    public void Recount(int newOnHand, string? note = null)
    {
        if (newOnHand < 0)
            throw new DomainException("Recount value cannot be negative.");
        if (newOnHand < Reserved)
            throw new DomainException($"Recount value {newOnHand} is less than Reserved {Reserved}. Release reservations first.");

        var delta = newOnHand - OnHand;
        OnHand = newOnHand;
        // Quantity field holds the *absolute new OnHand* for Recount rows
        // so the audit log shows what the count was set to, not just the delta.
        Record(newOnHand, delta, 0, InventoryMovementReason.Recount, null, note);
        Touch();
    }

    public void SetLowStockThreshold(int threshold)
    {
        if (threshold < 0)
            throw new DomainException("LowStockThreshold cannot be negative.");
        LowStockThreshold = threshold;
        Touch();
    }

    // ───────── helpers ─────────

    private static void RequirePositive(int qty)
    {
        if (qty <= 0)
            throw new DomainException("Quantity must be greater than zero.");
    }

    private void Record(int qty, int onHandDelta, int reservedDelta,
        InventoryMovementReason reason, Guid? referenceId, string? note)
    {
        _movements.Add(new InventoryMovement(Id, qty, onHandDelta, reservedDelta, reason, referenceId, note));
    }
}
