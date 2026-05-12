using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;


namespace ProductService.Application.Inventory.Commands.CommitStock;

// Order fulfilled: stock physically ships. Decreases OnHand AND Reserved.
// Will be called by the Order service when orders transition to Shipped status.
public sealed record CommitStockCommand(
    Guid ProductId,
    int Quantity,
    Guid OrderId,
    string? Note = null) : IRequest<InventoryItemDto>;

public sealed class CommitStockValidator : AbstractValidator<CommitStockCommand>
{
    public CommitStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class CommitStockHandler(IInventoryRepository repo)
    : IRequestHandler<CommitStockCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(CommitStockCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.Commit(request.Quantity, request.OrderId, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }

}

