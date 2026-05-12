namespace ProductService.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested aggregate does not exist. Mapped to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with id '{key}' was not found.") { }
}
