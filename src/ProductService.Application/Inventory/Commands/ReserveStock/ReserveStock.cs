using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.ReserveStock;

// Hold units against an active cart or pending order.
// Throws via DomainException if not enough Available stock.
public sealed record ReserveStockCommand(
    Guid ProductId,
    int Quantity,
    Guid ReferenceId,           // cart id or order id - REQUIRED so we can release later
    string? Note = null) : IRequest<InventoryItemDto>;

public sealed class ReserveStockValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ReferenceId).NotEmpty().WithMessage("ReferenceId (cart/order id) is required for reservations.");
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class ReserveStockHandler(IInventoryRepository repo)
    : IRequestHandler<ReserveStockCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(ReserveStockCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.Reserve(request.Quantity, request.ReferenceId, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
