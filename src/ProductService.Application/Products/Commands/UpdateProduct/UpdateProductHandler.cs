using MediatR;
using ProductService.Application.Common.Exceptions;
using ProductService.Application.Interfaces;
using ProductService.Application.Products.Dtos;

namespace ProductService.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepo,
    IInventoryRepository inventoryRepo)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);

        // Catalog data goes on the Product (name, description, price, currency).
        product.UpdateDetails(request.Name, request.Description, request.Price, request.Currency);

        // Stock changes route through the Inventory aggregate so the movement
        // history captures the change with a proper Recount reason. The legacy
        // Product.StockQuantity column is kept in sync as a denormalized cache
        // so any older code path still reads a plausible value.
        var inventory = await inventoryRepo.GetByProductIdAsync(product.Id, cancellationToken);
        if (inventory is not null && request.StockQuantity != inventory.OnHand)
        {
            inventory.Recount(request.StockQuantity, note: "Admin stock edit");
        }

        // Sync the legacy column (will be removed in a future migration).
        var legacyDelta = request.StockQuantity - product.StockQuantity;
        if (legacyDelta != 0)
        {
            product.AdjustStock(legacyDelta);
        }

        await productRepo.SaveChangesAsync(cancellationToken);

        return ProductDto.FromEntities(product, inventory);
    }
}
