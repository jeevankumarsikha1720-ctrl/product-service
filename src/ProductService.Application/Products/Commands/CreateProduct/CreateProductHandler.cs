using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Products.Dtos;
using ProductService.Domain.Entities;
using ProductService.Domain.Inventory;

namespace ProductService.Application.Products.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository productRepo,
    IInventoryRepository inventoryRepo)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.Currency,
            request.StockQuantity);

        await productRepo.AddAsync(product, cancellationToken);

        // Every new product gets a corresponding InventoryItem so we have
        // a single source of truth for stock from day one. Initial OnHand
        // mirrors what was specified in the create form.
        var inventory = InventoryItem.Create(
            productId: product.Id,
            initialOnHand: request.StockQuantity,
            lowStockThreshold: 0);

        await inventoryRepo.AddAsync(inventory, cancellationToken);

        // One save, one transaction - Product and InventoryItem land together
        // or neither does. SaveChanges on either repo writes the full change set.
        await productRepo.SaveChangesAsync(cancellationToken);

        return ProductDto.FromEntity(product);
    }
}
