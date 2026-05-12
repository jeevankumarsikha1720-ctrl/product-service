using MediatR;
using ProductService.Application.Products.Dtos;

namespace ProductService.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity) : IRequest<ProductDto>;
