using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentForRazorpay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_UpiTransactionRef",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "QrCodeData",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpiId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpiTransactionRef",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "UpiIntentUrl",
                table: "Payments",
                newName: "RazorpayResponseJson");

            migrationBuilder.RenameColumn(
                name: "TransactionNote",
                table: "Payments",
                newName: "Receipt");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "Payments",
                newName: "Wallet");

            migrationBuilder.RenameColumn(
                name: "PayeeName",
                table: "Payments",
                newName: "VPA");

            migrationBuilder.AddColumn<string>(
                name: "Bank",
                table: "Payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "Payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ErrorDescription",
                table: "Payments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MethodType",
                table: "Payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RazorpayOrderId",
                table: "Payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RazorpayPaymentId",
                table: "Payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RazorpaySignature",
                table: "Payments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RefundId",
                table: "Payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Payments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RazorpayOrderId",
                table: "Payments",
                column: "RazorpayOrderId",
                filter: "RazorpayOrderId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RazorpayPaymentId",
                table: "Payments",
                column: "RazorpayPaymentId",
                filter: "RazorpayPaymentId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_RazorpayOrderId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_RazorpayPaymentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Bank",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ErrorDescription",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "MethodType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RazorpayOrderId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RazorpayPaymentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RazorpaySignature",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "Wallet",
                table: "Payments",
                newName: "TransactionId");

            migrationBuilder.RenameColumn(
                name: "VPA",
                table: "Payments",
                newName: "PayeeName");

            migrationBuilder.RenameColumn(
                name: "Receipt",
                table: "Payments",
                newName: "TransactionNote");

            migrationBuilder.RenameColumn(
                name: "RazorpayResponseJson",
                table: "Payments",
                newName: "UpiIntentUrl");

            migrationBuilder.AddColumn<string>(
                name: "QrCodeData",
                table: "Payments",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UpiId",
                table: "Payments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UpiTransactionRef",
                table: "Payments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId",
                filter: "TransactionId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UpiTransactionRef",
                table: "Payments",
                column: "UpiTransactionRef",
                filter: "UpiTransactionRef IS NOT NULL");
        }
    }
}
