using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.AdjustStockIn;

// Manual positive correction (recount surplus, data entry fix).
public sealed record AdjustStockInCommand(
    Guid ProductId,
    int Quantity,
    string Note) : IRequest<InventoryItemDto>;

public sealed class AdjustStockInValidator : AbstractValidator<AdjustStockInCommand>
{
    public AdjustStockInValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).NotEmpty().MaximumLength(500)
            .WithMessage("A note is required for manual adjustments (audit trail).");
    }
}

public sealed class AdjustStockInHandler(IInventoryRepository repo)
    : IRequestHandler<AdjustStockInCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(AdjustStockInCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.AdjustIn(request.Quantity, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
