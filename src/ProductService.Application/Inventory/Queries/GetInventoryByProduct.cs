using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Queries.GetInventoryByProduct;

public sealed record GetInventoryByProductQuery(Guid ProductId)
    : IRequest<InventoryItemDto?>;

public sealed class GetInventoryByProductHandler(IInventoryRepository repo)
    : IRequestHandler<GetInventoryByProductQuery, InventoryItemDto?>
{
    public async Task<InventoryItemDto?> Handle(
        GetInventoryByProductQuery request,
        CancellationToken ct)
    {
        var item = await repo.GetByProductIdAsync(request.ProductId, ct);

        return item is null
            ? null
            : InventoryItemDto.FromEntity(item);
    }
}
