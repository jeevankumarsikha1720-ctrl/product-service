using FluentAssertions;
using NSubstitute;
using ProductService.Application.Interfaces;
using ProductService.Application.Products.Commands.CreateProduct;
using ProductService.Domain.Entities;
using ProductService.Domain.Inventory;
using Xunit;

namespace ProductService.Tests.Application;

public class CreateProductHandlerTests
{
    [Fact]
    public async Task Handle_PersistsProductAndReturnsDto()
    {
        var productRepo = Substitute.For<IProductRepository>();
        var inventoryRepo = Substitute.For<IInventoryRepository>();
        var handler = new CreateProductHandler(productRepo, inventoryRepo);
        var command = new CreateProductCommand("Widget", "Useful", 12.50m, "USD", 100);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Widget");
        result.Price.Should().Be(12.50m);
        result.Currency.Should().Be("USD");
        result.StockQuantity.Should().Be(100);

        await productRepo.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await inventoryRepo.Received(1).AddAsync(Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await productRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GeneratesUniqueIdsForDifferentProducts()
    {
        var productRepo = Substitute.For<IProductRepository>();
        var inventoryRepo = Substitute.For<IInventoryRepository>();
        var handler = new CreateProductHandler(productRepo, inventoryRepo);

        var first = await handler.Handle(
            new CreateProductCommand("A", "x", 1m, "USD", 1), CancellationToken.None);
        var second = await handler.Handle(
            new CreateProductCommand("B", "y", 2m, "USD", 2), CancellationToken.None);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task Handle_CreatesInventoryItemWithMatchingInitialStock()
    {
        var productRepo = Substitute.For<IProductRepository>();
        var inventoryRepo = Substitute.For<IInventoryRepository>();
        var handler = new CreateProductHandler(productRepo, inventoryRepo);

        InventoryItem? captured = null;
        await inventoryRepo.AddAsync(
            Arg.Do<InventoryItem>(i => captured = i),
            Arg.Any<CancellationToken>());

        await handler.Handle(
            new CreateProductCommand("Widget", "Useful", 1m, "USD", 42),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.OnHand.Should().Be(42);
        captured.Reserved.Should().Be(0);
        captured.Available.Should().Be(42);
    }
}
