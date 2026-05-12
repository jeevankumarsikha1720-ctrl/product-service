using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_InventoryItemId",
                schema: "inventory",
                table: "InventoryMovements");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_InventoryItemId_OccurredAtUtc",
                schema: "inventory",
                table: "InventoryMovements",
                columns: new[] { "InventoryItemId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Reason",
                schema: "inventory",
                table: "InventoryMovements",
                column: "Reason");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ReferenceId",
                schema: "inventory",
                table: "InventoryMovements",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProductId",
                schema: "inventory",
                table: "InventoryItems",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_InventoryItemId_OccurredAtUtc",
                schema: "inventory",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_Reason",
                schema: "inventory",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_ReferenceId",
                schema: "inventory",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ProductId",
                schema: "inventory",
                table: "InventoryItems");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_InventoryItemId",
                schema: "inventory",
                table: "InventoryMovements",
                column: "InventoryItemId");
        }
    }
}
