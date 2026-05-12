using MediatR;
using ProductService.Application.Common.Exceptions;
using ProductService.Application.Interfaces;
using ProductService.Application.Products.Dtos;

namespace ProductService.Application.Products.Queries.GetProduct;

public sealed class GetProductHandler(
    IProductRepository productRepo,
    IInventoryRepository inventoryRepo)
    : IRequestHandler<GetProductQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);

        // Load the matching inventory record so the DTO carries live Available/OnHand/Reserved.
        var inventory = await inventoryRepo.GetByProductIdAsync(product.Id, cancellationToken);

        return ProductDto.FromEntities(product, inventory);
    }
}
