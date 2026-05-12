namespace ProductService.Domain.Exceptions;

/// <summary>
/// Thrown when a business rule defined in the domain layer is violated.
/// API layer translates this to HTTP 400.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
