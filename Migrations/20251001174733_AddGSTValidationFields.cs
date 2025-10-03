using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class AddGSTValidationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GSTEntityNumber",
                table: "Shops",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GSTEntityType",
                table: "Shops",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GSTPANNumber",
                table: "Shops",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GSTStateCode",
                table: "Shops",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GSTStateName",
                table: "Shops",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "GSTValidatedAt",
                table: "Shops",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GSTEntityNumber",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "GSTEntityType",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "GSTPANNumber",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "GSTStateCode",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "GSTStateName",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "GSTValidatedAt",
                table: "Shops");
        }
    }
}
