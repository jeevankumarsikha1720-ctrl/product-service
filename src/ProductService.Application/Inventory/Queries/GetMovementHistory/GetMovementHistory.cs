using FluentValidation;
using MediatR;
using ProductService.Application.Common.Models;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Queries.GetMovementHistory;

// Paged audit log for one InventoryItem, newest first.
public sealed record GetMovementHistoryQuery(
    Guid InventoryItemId,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<InventoryMovementDto>>;

public sealed class GetMovementHistoryValidator : AbstractValidator<GetMovementHistoryQuery>
{
    public GetMovementHistoryValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public sealed class GetMovementHistoryHandler(IInventoryRepository repo)
    : IRequestHandler<GetMovementHistoryQuery, PagedResult<InventoryMovementDto>>
{
    public async Task<PagedResult<InventoryMovementDto>> Handle(
        GetMovementHistoryQuery request, CancellationToken ct)
    {
        var page = await repo.ListMovementsAsync(
            request.InventoryItemId, request.Page, request.PageSize, ct);
        return page.Map(InventoryMovementDto.FromEntity);
    }
}
