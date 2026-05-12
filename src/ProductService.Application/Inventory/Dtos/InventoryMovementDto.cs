using ProductService.Domain.Inventory;

namespace ProductService.Application.Inventory.Dtos;

public sealed record InventoryMovementDto(
    Guid Id,
    Guid InventoryItemId,
    int Quantity,
    int OnHandDelta,
    int ReservedDelta,
    InventoryMovementReason Reason,
    string ReasonLabel,
    Guid? ReferenceId,
    string? Note,
    DateTime OccurredAtUtc)
{
    public static InventoryMovementDto FromEntity(InventoryMovement m) => new(
        m.Id, m.InventoryItemId, m.Quantity, m.OnHandDelta, m.ReservedDelta,
        m.Reason, m.Reason.ToString(),
        m.ReferenceId, m.Note, m.OccurredAtUtc);
}
