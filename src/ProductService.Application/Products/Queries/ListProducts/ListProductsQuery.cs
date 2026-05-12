using MediatR;
using ProductService.Application.Common.Models;
using ProductService.Application.Products.Dtos;

namespace ProductService.Application.Products.Queries.ListProducts;

public sealed record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<PagedResult<ProductDto>>;
