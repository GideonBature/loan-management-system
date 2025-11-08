using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLoanEntityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Frequency",
                table: "Loans",
                newName: "EmploymentStatus");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyIncome",
                table: "Loans",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyIncome",
                table: "Loans");

            migrationBuilder.RenameColumn(
                name: "EmploymentStatus",
                table: "Loans",
                newName: "Frequency");
        }
    }
}
