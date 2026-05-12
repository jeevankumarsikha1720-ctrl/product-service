using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.WriteOffStock;

// Stock damaged, expired, stolen, or otherwise unsellable. Decreases OnHand.
// Cannot write off units that are currently Reserved - release the reservation first.
public sealed record WriteOffStockCommand(
    Guid ProductId,
    int Quantity,
    string Reason) : IRequest<InventoryItemDto>;

public sealed class WriteOffStockValidator : AbstractValidator<WriteOffStockCommand>
{
    public WriteOffStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("A write-off reason is required (e.g. 'Damaged in transit', 'Past expiry').");
    }
}

public sealed class WriteOffStockHandler(IInventoryRepository repo)
    : IRequestHandler<WriteOffStockCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(WriteOffStockCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.WriteOff(request.Quantity, request.Reason);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
