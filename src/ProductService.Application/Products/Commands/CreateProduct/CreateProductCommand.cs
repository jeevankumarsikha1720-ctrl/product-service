using MediatR;
using ProductService.Application.Products.Dtos;

namespace ProductService.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity) : IRequest<ProductDto>;
