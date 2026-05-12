using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Inventory;

namespace ProductService.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration
    : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems", "inventory");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
        .ValueGeneratedNever();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.OnHand)
            .IsRequired();

        builder.Property(x => x.Reserved)
            .IsRequired();

        builder.Property(x => x.LowStockThreshold)
            .IsRequired();

        builder.Ignore(x => x.Available);
        builder.Ignore(x => x.IsLowStock);

        // One InventoryItem per Product (1:1 today; relax when we add warehouse locations).
        builder.HasIndex(x => x.ProductId).IsUnique();

        builder.HasMany(x => x.Movements)
            .WithOne()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Movements)
        .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
