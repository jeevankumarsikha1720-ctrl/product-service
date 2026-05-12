using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.AdjustStockOut;

// Manual negative correction (recount shortage, theft, data entry fix).
public sealed record AdjustStockOutCommand(
    Guid ProductId,
    int Quantity,
    string Note) : IRequest<InventoryItemDto>;

public sealed class AdjustStockOutValidator : AbstractValidator<AdjustStockOutCommand>
{
    public AdjustStockOutValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).NotEmpty().MaximumLength(500)
            .WithMessage("A note is required for manual adjustments (audit trail).");
    }
}

public sealed class AdjustStockOutHandler(IInventoryRepository repo)
    : IRequestHandler<AdjustStockOutCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(AdjustStockOutCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.AdjustOut(request.Quantity, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
