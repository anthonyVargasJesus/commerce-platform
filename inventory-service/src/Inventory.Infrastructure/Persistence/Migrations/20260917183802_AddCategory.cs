using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                schema: "inventory",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                schema: "inventory",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_name",
                schema: "inventory",
                table: "categories",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_products_categories_category_id",
                schema: "inventory",
                table: "products",
                column: "category_id",
                principalSchema: "inventory",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_categories_category_id",
                schema: "inventory",
                table: "products");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_products_category_id",
                schema: "inventory",
                table: "products");

            migrationBuilder.DropColumn(
                name: "category_id",
                schema: "inventory",
                table: "products");
        }
    }
}
