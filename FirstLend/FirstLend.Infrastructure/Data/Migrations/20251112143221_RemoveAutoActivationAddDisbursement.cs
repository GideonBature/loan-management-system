using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAutoActivationAddDisbursement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisbursedAt",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisbursedAt",
                table: "Loans");
        }
    }
}
