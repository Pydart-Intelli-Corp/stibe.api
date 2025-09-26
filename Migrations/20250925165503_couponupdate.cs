using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class couponupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CouponType",
                table: "UserCouponUsages",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "UserCouponUsages",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "UserCouponUsages",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "UserCouponUsages",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SavingsAmount",
                table: "UserCouponUsages",
                type: "decimal(65,30)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CouponType",
                table: "UserCouponUsages");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "UserCouponUsages");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "UserCouponUsages");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "UserCouponUsages");

            migrationBuilder.DropColumn(
                name: "SavingsAmount",
                table: "UserCouponUsages");
        }
    }
}
