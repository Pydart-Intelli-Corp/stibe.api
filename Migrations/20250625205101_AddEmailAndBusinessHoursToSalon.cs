using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAndBusinessHoursToSalon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessHours",
                table: "Salons",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Salons",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessHours",
                table: "Salons");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Salons");
        }
    }
}
