using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.RecountStock;

// Physical recount: replace OnHand with the new absolute value.
// Throws if the recount value is less than currently Reserved units.
public sealed record RecountStockCommand(
    Guid ProductId,
    int NewOnHand,
    string Note) : IRequest<InventoryItemDto>;

public sealed class RecountStockValidator : AbstractValidator<RecountStockCommand>
{
    public RecountStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NewOnHand).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Note).NotEmpty().MaximumLength(500)
            .WithMessage("A note is required for recounts (audit trail).");
    }
}

public sealed class RecountStockHandler(IInventoryRepository repo)
    : IRequestHandler<RecountStockCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(RecountStockCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.Recount(request.NewOnHand, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
