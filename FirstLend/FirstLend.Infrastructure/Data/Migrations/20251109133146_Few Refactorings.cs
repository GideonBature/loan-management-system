using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FewRefactorings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "PaymentHistories",
                newName: "AmountDue");

            migrationBuilder.AddColumn<string>(
                name: "DueAt",
                table: "PaymentHistories",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "PaymentHistories");

            migrationBuilder.RenameColumn(
                name: "AmountDue",
                table: "PaymentHistories",
                newName: "Amount");
        }
    }
}
