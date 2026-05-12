using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.ReturnFromSale;

// Customer returned a delivered item in resellable condition. OnHand goes back up.
public sealed record ReturnFromSaleCommand(
    Guid ProductId,
    int Quantity,
    Guid OrderId,
    string? Note = null) : IRequest<InventoryItemDto>;

public sealed class ReturnFromSaleValidator : AbstractValidator<ReturnFromSaleCommand>
{
    public ReturnFromSaleValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class ReturnFromSaleHandler(IInventoryRepository repo)
    : IRequestHandler<ReturnFromSaleCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(ReturnFromSaleCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.ReturnFromSale(request.Quantity, request.OrderId, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
