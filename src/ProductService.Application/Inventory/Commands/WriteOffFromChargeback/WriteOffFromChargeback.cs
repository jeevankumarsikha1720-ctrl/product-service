using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.WriteOffFromChargeback;

// Chargeback received but the item was NOT returned (friendly fraud or lost in transit).
// OnHand was already decremented when the sale committed - this is purely
// informational, tagging the loss so accounting can reconcile it.
public sealed record WriteOffFromChargebackCommand(
    Guid ProductId,
    int Quantity,
    Guid OrderId,
    string? Note = null) : IRequest<InventoryItemDto>;

public sealed class WriteOffFromChargebackValidator : AbstractValidator<WriteOffFromChargebackCommand>
{
    public WriteOffFromChargebackValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class WriteOffFromChargebackHandler(IInventoryRepository repo)
    : IRequestHandler<WriteOffFromChargebackCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(WriteOffFromChargebackCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.WriteOffFromChargeback(request.Quantity, request.OrderId, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
