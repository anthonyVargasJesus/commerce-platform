using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeSkuUniquenessActiveOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_sku",
                schema: "inventory",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_products_sku",
                schema: "inventory",
                table: "products",
                column: "sku",
                unique: true,
                filter: "is_active = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_sku",
                schema: "inventory",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_products_sku",
                schema: "inventory",
                table: "products",
                column: "sku",
                unique: true);
        }
    }
}
