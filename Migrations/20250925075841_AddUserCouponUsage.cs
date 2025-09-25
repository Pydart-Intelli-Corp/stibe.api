using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCouponUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserCouponUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CouponCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsEmailSent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    MaxUsageLimit = table.Column<int>(type: "int", nullable: false),
                    IsBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    BlockReason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCouponUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCouponUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserCouponUsages_Email_IsEmailSent",
                table: "UserCouponUsages",
                columns: new[] { "Email", "IsEmailSent" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCouponUsages_Email_PhoneNumber_Purpose",
                table: "UserCouponUsages",
                columns: new[] { "Email", "PhoneNumber", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCouponUsages_IsBlocked",
                table: "UserCouponUsages",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_UserCouponUsages_IsDeleted",
                table: "UserCouponUsages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserCouponUsages_UserId_CouponCode",
                table: "UserCouponUsages",
                columns: new[] { "UserId", "CouponCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCouponUsages");
        }
    }
}
