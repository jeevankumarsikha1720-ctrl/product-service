using FluentAssertions;
using ProductService.Domain.Entities;
using ProductService.Domain.Exceptions;
using Xunit;

namespace ProductService.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidArguments_ProducesActiveProduct()
    {
        var product = Product.Create("Widget", "A useful widget", 9.99m, "usd", 10);

        product.Name.Should().Be("Widget");
        product.Description.Should().Be("A useful widget");
        product.Price.Should().Be(9.99m);
        product.Currency.Should().Be("USD");
        product.StockQuantity.Should().Be(10);
        product.IsActive.Should().BeTrue();
        product.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankName_Throws(string? name)
    {
        var act = () => Product.Create(name!, "desc", 1m, "USD", 1);
        act.Should().Throw<DomainException>().WithMessage("*name is required*");
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        var act = () => Product.Create("Widget", "d", -1m, "USD", 1);
        act.Should().Throw<DomainException>().WithMessage("*Price cannot be negative*");
    }

    [Fact]
    public void AdjustStock_DecreasingBelowZero_Throws()
    {
        var product = Product.Create("Widget", "d", 1m, "USD", 5);
        var act = () => product.AdjustStock(-10);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AdjustStock_PositiveDelta_IncreasesStockAndTouchesUpdatedAt()
    {
        var product = Product.Create("Widget", "d", 1m, "USD", 5);

        product.AdjustStock(3);

        product.StockQuantity.Should().Be(8);
        product.UpdatedAtUtc.Should().NotBeNull();
    }
}
