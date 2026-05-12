using FluentValidation;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Application.Inventory.Dtos;

namespace ProductService.Application.Inventory.Commands.SetLowStockThreshold;

public sealed record SetLowStockThresholdCommand(
    Guid ProductId,
    int Threshold) : IRequest<InventoryItemDto>;

public sealed class SetLowStockThresholdValidator : AbstractValidator<SetLowStockThresholdCommand>
{
    public SetLowStockThresholdValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Threshold).GreaterThanOrEqualTo(0);
    }
}

public sealed class SetLowStockThresholdHandler(IInventoryRepository repo)
    : IRequestHandler<SetLowStockThresholdCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(SetLowStockThresholdCommand request, CancellationToken ct)
    {
        var item = await repo.LoadByProductOrThrowAsync(request.ProductId, ct);
        item.SetLowStockThreshold(request.Threshold);
        await repo.SaveChangesAsync(ct);
        return InventoryItemDto.FromEntity(item);
    }
}
