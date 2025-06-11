using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffWorkSessions1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffWorkSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClockInTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    ClockOutTime = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    ScheduledMinutes = table.Column<int>(type: "int", nullable: false),
                    ActualMinutes = table.Column<int>(type: "int", nullable: false),
                    BreakMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClockInLatitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    ClockInLongitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    ClockOutLatitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    ClockOutLongitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    ServicesCompleted = table.Column<int>(type: "int", nullable: false),
                    RevenueGenerated = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CommissionEarned = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffWorkSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffWorkSessions_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StaffWorkSessions_StaffId_WorkDate",
                table: "StaffWorkSessions",
                columns: new[] { "StaffId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffWorkSessions_Status",
                table: "StaffWorkSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StaffWorkSessions_WorkDate",
                table: "StaffWorkSessions",
                column: "WorkDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffWorkSessions");
        }
    }
}
