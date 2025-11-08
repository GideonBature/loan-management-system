using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBorrowerIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Loans_User_BorrowerId",
                table: "Loans");

            migrationBuilder.AlterColumn<string>(
                name: "BorrowerId",
                table: "Loans",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_AspNetUsers_BorrowerId",
                table: "Loans",
                column: "BorrowerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Loans_AspNetUsers_BorrowerId",
                table: "Loans");

            migrationBuilder.AlterColumn<Guid>(
                name: "BorrowerId",
                table: "Loans",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_User_BorrowerId",
                table: "Loans",
                column: "BorrowerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
