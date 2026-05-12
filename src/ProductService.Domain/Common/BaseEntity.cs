namespace ProductService.Domain.Common;

/// <summary>
/// Base entity providing audit fields and a strongly-typed Guid id.
/// Keep this layer free of EF Core / framework dependencies.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; protected set; }

    public void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
