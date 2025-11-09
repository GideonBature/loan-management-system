using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentHistoryUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentHistories_User_UserId",
                table: "PaymentHistories");

            migrationBuilder.DropIndex(
                name: "IX_PaymentHistories_UserId",
                table: "PaymentHistories");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PaymentHistories",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PaymentHistories",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentHistories_UserId",
                table: "PaymentHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentHistories_User_UserId",
                table: "PaymentHistories",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
