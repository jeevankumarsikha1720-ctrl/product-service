using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Inventory;

namespace ProductService.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements", "inventory");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.InventoryItemId).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.OnHandDelta).IsRequired();
        builder.Property(x => x.ReservedDelta).IsRequired();

        // Store the reason as its readable string name ("Reserved", "Sold", etc.) instead
        // of an int. Slightly less compact but enormously more useful when staring at
        // the DB during a chargeback investigation.
        builder.Property(x => x.Reason)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ReferenceId);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.Property(x => x.OccurredAtUtc).IsRequired();

        builder.HasIndex(x => new { x.InventoryItemId, x.OccurredAtUtc });
        builder.HasIndex(x => x.ReferenceId);
        builder.HasIndex(x => x.Reason);
    }
}
