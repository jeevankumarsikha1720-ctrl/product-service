using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.ReturnFromChargeback;

// Chargeback received AND the item physically came back. Like a normal return,
// but tagged with a chargeback-specific reason for audit/reporting.
public sealed record ReturnFromChargebackCommand(
    Guid ProductId,
    int Quantity,
    Guid OrderId,
    string? Note = null) : IRequest<InventoryItemDto>;

public sealed class ReturnFromChargebackValidator : AbstractValidator<ReturnFromChargebackCommand>
{
    public ReturnFromChargebackValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class ReturnFromChargebackHandler(IInventoryRepository repo)
    : IRequestHandler<ReturnFromChargebackCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(ReturnFromChargebackCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.ReturnFromChargeback(request.Quantity, request.OrderId, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
