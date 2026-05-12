using MediatR;
using ProductService.Application.Products.Dtos;

namespace ProductService.Application.Products.Queries.GetProduct;

public sealed record GetProductQuery(Guid Id) : IRequest<ProductDto>;
