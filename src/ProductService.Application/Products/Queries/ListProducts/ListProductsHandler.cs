using MediatR;
using ProductService.Application.Common.Models;
using ProductService.Application.Interfaces;
using ProductService.Application.Products.Dtos;

namespace ProductService.Application.Products.Queries.ListProducts;

public sealed class ListProductsHandler(
    IProductRepository productRepo,
    IInventoryRepository inventoryRepo)
    : IRequestHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await productRepo.ListAsync(
            request.Page, request.PageSize, request.Search, cancellationToken);

        // Batch fetch inventory for every product in this page - one extra query,
        // not one per product. Avoids the N+1 trap.
        var inventoryByProduct = await inventoryRepo.GetByProductIdsAsync(
            items.Select(p => p.Id), cancellationToken);

        var dtos = items
            .Select(p => ProductDto.FromEntities(
                p,
                inventoryByProduct.TryGetValue(p.Id, out var inv) ? inv : null))
            .ToList();

        return new PagedResult<ProductDto>(dtos, request.Page, request.PageSize, totalCount);
    }
}
