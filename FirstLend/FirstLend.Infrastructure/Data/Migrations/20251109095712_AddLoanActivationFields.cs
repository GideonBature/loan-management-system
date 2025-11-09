using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanActivationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Loans");
        }
    }
}
