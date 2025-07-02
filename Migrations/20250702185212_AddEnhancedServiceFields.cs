using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedServiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OfferPrice",
                table: "Services",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductsUsed",
                table: "Services",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ServiceImages",
                table: "Services",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfferPrice",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ProductsUsed",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ServiceImages",
                table: "Services");
        }
    }
}
