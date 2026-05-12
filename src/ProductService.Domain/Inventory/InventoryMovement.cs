namespace ProductService.Domain.Inventory;

/// <summary>
/// One row in the inventory audit log. Immutable - constructed by the
/// InventoryItem aggregate when it applies a change, never edited afterward.
///
/// Together with InventoryItem.OnHand/Reserved these rows form a complete
/// history: replaying every movement chronologically should reproduce the
/// current state of the aggregate.
/// </summary>
public sealed class InventoryMovement
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>The inventory item this movement applies to.</summary>
    public Guid InventoryItemId { get; private set; }

    /// <summary>Quantity moved. Always positive. The reason determines the direction.</summary>
    public int Quantity { get; private set; }

    /// <summary>Net change to OnHand caused by this movement (can be negative or zero).</summary>
    public int OnHandDelta { get; private set; }

    /// <summary>Net change to Reserved caused by this movement (can be negative or zero).</summary>
    public int ReservedDelta { get; private set; }

    public InventoryMovementReason Reason { get; private set; }

    /// <summary>
    /// Optional link to the entity that triggered this movement
    /// (cart id, order id, supplier shipment id, etc.).
    /// </summary>
    public Guid? ReferenceId { get; private set; }

    /// <summary>Free-text note for context (e.g., "Damaged in transit", "Recount after stocktake").</summary>
    public string? Note { get; private set; }

    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;

    // EF Core
    private InventoryMovement() { }

    internal InventoryMovement(
        Guid inventoryItemId,
        int quantity,
        int onHandDelta,
        int reservedDelta,
        InventoryMovementReason reason,
        Guid? referenceId,
        string? note)
    {
        InventoryItemId = inventoryItemId;
        Quantity = quantity;
        OnHandDelta = onHandDelta;
        ReservedDelta = reservedDelta;
        Reason = reason;
        ReferenceId = referenceId;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
