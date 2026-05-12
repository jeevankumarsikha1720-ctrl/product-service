using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;
using ProductService.Domain.Inventory;

namespace ProductService.Application.Inventory.Commands.ReceiveStock;

public sealed record ReceiveStockCommand(
    Guid ProductId,
    int Quantity,
    string? Note = null)
    : IRequest<InventoryItemDto>;

public sealed class ReceiveStockValidator
    : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}

public sealed class ReceiveStockHandler(IInventoryRepository repo)
    : IRequestHandler<ReceiveStockCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(
        ReceiveStockCommand request,
        CancellationToken ct)
    {
        var item = await repo.GetByProductIdAsync(
            request.ProductId,
            ct);

        if (item is null)
        {
            item = InventoryItem.Create(
                request.ProductId,
                request.Quantity);

            await repo.AddAsync(item, ct);
        }
        else
        {
            item.Receive(
                request.Quantity,
                referenceId: null,
                note: request.Note);
        }

        await repo.SaveChangesAsync(ct);

        return InventoryItemDto.FromEntity(item);
    }
}
