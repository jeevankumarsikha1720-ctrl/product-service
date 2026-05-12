using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.ReleaseStock;

// Release a prior reservation. Cart abandoned, order cancelled, reservation timed out.
public sealed record ReleaseStockCommand(
    Guid ProductId,
    int Quantity,
    Guid ReferenceId,
    string? Note = null) : IRequest<InventoryItemDto>;

public sealed class ReleaseStockValidator : AbstractValidator<ReleaseStockCommand>
{
    public ReleaseStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ReferenceId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class ReleaseStockHandler(IInventoryRepository repo)
    : IRequestHandler<ReleaseStockCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(ReleaseStockCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.Release(request.Quantity, request.ReferenceId, request.Note);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
