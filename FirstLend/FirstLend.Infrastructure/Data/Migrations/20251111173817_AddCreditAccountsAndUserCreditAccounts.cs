using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditAccountsAndUserCreditAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepaymentEvent_CreditAccounts_CreditAccountId",
                table: "RepaymentEvent");

            migrationBuilder.DropIndex(
                name: "IX_UserCreditAccounts_UserId",
                table: "UserCreditAccounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreditAccountId",
                table: "RepaymentEvent",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditAccounts_UserId_CreditAccountId",
                table: "UserCreditAccounts",
                columns: new[] { "UserId", "CreditAccountId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RepaymentEvent_CreditAccounts_CreditAccountId",
                table: "RepaymentEvent",
                column: "CreditAccountId",
                principalTable: "CreditAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepaymentEvent_CreditAccounts_CreditAccountId",
                table: "RepaymentEvent");

            migrationBuilder.DropIndex(
                name: "IX_UserCreditAccounts_UserId_CreditAccountId",
                table: "UserCreditAccounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreditAccountId",
                table: "RepaymentEvent",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditAccounts_UserId",
                table: "UserCreditAccounts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepaymentEvent_CreditAccounts_CreditAccountId",
                table: "RepaymentEvent",
                column: "CreditAccountId",
                principalTable: "CreditAccounts",
                principalColumn: "Id");
        }
    }
}
