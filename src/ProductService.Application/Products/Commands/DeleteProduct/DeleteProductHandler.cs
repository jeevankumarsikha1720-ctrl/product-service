using MediatR;
using ProductService.Application.Common.Exceptions;
using ProductService.Application.Interfaces;

namespace ProductService.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductHandler(IProductRepository repository)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);

        repository.Remove(product);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
