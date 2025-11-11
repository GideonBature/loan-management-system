using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreateAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Institution = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PerformanceStatus = table.Column<int>(type: "integer", nullable: false),
                    DateOpened = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    RepaymentAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepaymentEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreditAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepaymentEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepaymentEvent_CreditAccounts_CreditAccountId",
                        column: x => x.CreditAccountId,
                        principalTable: "CreditAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserCreditAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCreditAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCreditAccounts_CreditAccounts_CreditAccountId",
                        column: x => x.CreditAccountId,
                        principalTable: "CreditAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCreditAccounts_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentEvent_CreditAccountId",
                table: "RepaymentEvent",
                column: "CreditAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditAccounts_CreditAccountId",
                table: "UserCreditAccounts",
                column: "CreditAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditAccounts_UserId",
                table: "UserCreditAccounts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepaymentEvent");

            migrationBuilder.DropTable(
                name: "UserCreditAccounts");

            migrationBuilder.DropTable(
                name: "CreditAccounts");
        }
    }
}
