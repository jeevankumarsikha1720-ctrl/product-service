using FluentValidation;

namespace ProductService.Application.Products.Commands.CreateProduct;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    private static readonly string[] AllowedCurrencies = { "USD", "EUR", "GBP", "INR", "AUD", "CAD" };

    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => AllowedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}
