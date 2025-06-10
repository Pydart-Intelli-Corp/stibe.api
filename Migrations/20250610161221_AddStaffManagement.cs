using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStaffActive",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SalonId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StaffJoinDate",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedStaffId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerFeedback",
                table: "Bookings",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "CustomerNotified",
                table: "Bookings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerNotifiedAt",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerTip",
                table: "Bookings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedEndTime",
                table: "Bookings",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedStartTime",
                table: "Bookings",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NextCustomerNotified",
                table: "Bookings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextCustomerNotifiedAt",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceCompletedAt",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceStartedAt",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceStatus",
                table: "Bookings",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "StaffCommissionEarned",
                table: "Bookings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StaffRating",
                table: "Bookings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bio = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhotoUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    LunchBreakStart = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    LunchBreakEnd = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    ExperienceYears = table.Column<int>(type: "int", nullable: false),
                    EfficiencyMultiplier = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EmploymentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AverageRating = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalReviews = table.Column<int>(type: "int", nullable: false),
                    TotalServices = table.Column<int>(type: "int", nullable: false),
                    SalonId = table.Column<int>(type: "int", nullable: false),
                    Certifications = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Languages = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstagramHandle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staff_Salons_SalonId",
                        column: x => x.SalonId,
                        principalTable: "Salons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Staff_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SalonId",
                table: "Users",
                column: "SalonId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AssignedStaffId",
                table: "Bookings",
                column: "AssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Email",
                table: "Staff",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_PhoneNumber",
                table: "Staff",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_SalonId_IsActive",
                table: "Staff",
                columns: new[] { "SalonId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_UserId",
                table: "Staff",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Staff_AssignedStaffId",
                table: "Bookings",
                column: "AssignedStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Salons_SalonId",
                table: "Users",
                column: "SalonId",
                principalTable: "Salons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Staff_AssignedStaffId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Salons_SalonId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Users_SalonId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AssignedStaffId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsStaffActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SalonId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StaffJoinDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedStaffId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerFeedback",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerNotified",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerNotifiedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerTip",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EstimatedEndTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EstimatedStartTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NextCustomerNotified",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NextCustomerNotifiedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ServiceCompletedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ServiceStartedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ServiceStatus",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StaffCommissionEarned",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StaffRating",
                table: "Bookings");
        }
    }
}
