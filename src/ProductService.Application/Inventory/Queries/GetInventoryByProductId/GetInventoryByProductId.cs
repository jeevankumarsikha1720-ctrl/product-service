using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Queries.GetInventoryByProductId;

public sealed record GetInventoryByProductIdQuery(Guid ProductId) : IRequest<InventoryItemDto>;

public sealed class GetInventoryByProductIdHandler(IInventoryRepository repo)
    : IRequestHandler<GetInventoryByProductIdQuery, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(GetInventoryByProductIdQuery request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        return InventoryItemDto.FromEntity(item);
    }
}
