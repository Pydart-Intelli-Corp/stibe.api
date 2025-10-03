using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class AddKycVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AadhaarImageUrl",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AadhaarNumber",
                table: "Users",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsKycVerified",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KycRejectionReason",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "KycStatus",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "KycSubmittedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KycVerifiedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KycVerifiedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PanImageUrl",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PanNumber",
                table: "Users",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AadhaarNumber",
                table: "Users",
                column: "AadhaarNumber",
                unique: true,
                filter: "AadhaarNumber IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PanNumber",
                table: "Users",
                column: "PanNumber",
                unique: true,
                filter: "PanNumber IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_AadhaarNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PanNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AadhaarImageUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AadhaarNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsKycVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycRejectionReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycSubmittedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycVerifiedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PanImageUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PanNumber",
                table: "Users");
        }
    }
}
