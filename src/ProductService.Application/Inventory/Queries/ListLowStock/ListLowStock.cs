using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Queries.ListLowStock;

// Powers the admin low-stock dashboard. Returns every InventoryItem whose
// OnHand has dropped to or below its LowStockThreshold.
public sealed record ListLowStockQuery() : IRequest<IReadOnlyList<InventoryItemDto>>;

public sealed class ListLowStockHandler(IInventoryRepository repo)
    : IRequestHandler<ListLowStockQuery, IReadOnlyList<InventoryItemDto>>
{
    public async Task<IReadOnlyList<InventoryItemDto>> Handle(ListLowStockQuery _, CancellationToken ct)
    {
        var items = await repo.ListLowStockAsync(ct);
        return items.Select(InventoryItemDto.FromEntity).ToList();
    }
}
