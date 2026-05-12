using ProductService.Domain.Common;
using ProductService.Domain.Exceptions;

namespace ProductService.Domain.Entities;

/// <summary>
/// Product aggregate root. State changes go through methods, not setters,
/// so business rules stay enforced everywhere the entity is used.
/// </summary>
public sealed class Product : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "USD";
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core needs a parameterless constructor.
    private Product() { }

    public static Product Create(string name, string? description, decimal price, string currency, int stockQuantity)
    {
        ValidateName(name);
        ValidatePrice(price);
        ValidateStock(stockQuantity);

        return new Product
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Price = price,
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant(),
            StockQuantity = stockQuantity
        };
    }

    public void UpdateDetails(string name, string? description, decimal price, string currency)
    {
        ValidateName(name);
        ValidatePrice(price);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? Currency : currency.ToUpperInvariant();
        Touch();
    }

    public void AdjustStock(int delta)
    {
        var newStock = StockQuantity + delta;
        if (newStock < 0)
            throw new DomainException($"Cannot reduce stock below 0 (current: {StockQuantity}, delta: {delta}).");
        StockQuantity = newStock;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");
        if (name.Length > 200)
            throw new DomainException("Product name cannot exceed 200 characters.");
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
            throw new DomainException("Price cannot be negative.");
    }

    private static void ValidateStock(int stockQuantity)
    {
        if (stockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");
    }
}
