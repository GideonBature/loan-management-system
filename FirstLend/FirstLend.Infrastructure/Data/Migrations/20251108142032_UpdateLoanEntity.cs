using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstLend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLoanEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Frequncy",
                table: "Loans",
                newName: "Frequency");

            // Convert Status from text to integer with explicit USING clause
            migrationBuilder.Sql(@"
                ALTER TABLE ""Loans"" 
                ALTER COLUMN ""Status"" TYPE integer 
                USING CASE 
                    WHEN ""Status"" = 'pending' OR ""Status"" = '0' THEN 0
                    WHEN ""Status"" = 'approved' OR ""Status"" = '1' THEN 1
                    WHEN ""Status"" = 'rejected' OR ""Status"" = '2' THEN 2
                    WHEN ""Status"" = 'active' OR ""Status"" = '3' THEN 3
                    WHEN ""Status"" = 'completed' OR ""Status"" = '4' THEN 4
                    WHEN ""Status"" = 'defaulted' OR ""Status"" = '5' THEN 5
                    ELSE 0
                END;
            ");

            // Convert NextPaymentDate from text to timestamp
            migrationBuilder.Sql(@"
                ALTER TABLE ""Loans"" 
                ALTER COLUMN ""NextPaymentDate"" TYPE timestamp with time zone 
                USING CASE 
                    WHEN ""NextPaymentDate"" IS NULL OR ""NextPaymentDate"" = '' THEN CURRENT_TIMESTAMP
                    ELSE ""NextPaymentDate""::timestamp with time zone
                END;
            ");

            // Convert DueAt from text to timestamp
            migrationBuilder.Sql(@"
                ALTER TABLE ""Loans"" 
                ALTER COLUMN ""DueAt"" TYPE timestamp with time zone 
                USING CASE 
                    WHEN ""DueAt"" IS NULL OR ""DueAt"" = '' THEN CURRENT_TIMESTAMP
                    ELSE ""DueAt""::timestamp with time zone
                END;
            ");

            // Convert CreatedAt from text to timestamp
            migrationBuilder.Sql(@"
                ALTER TABLE ""Loans"" 
                ALTER COLUMN ""CreatedAt"" TYPE timestamp with time zone 
                USING CASE 
                    WHEN ""CreatedAt"" IS NULL OR ""CreatedAt"" = '' THEN CURRENT_TIMESTAMP
                    ELSE ""CreatedAt""::timestamp with time zone
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Frequency",
                table: "Loans",
                newName: "Frequncy");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Loans",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "NextPaymentDate",
                table: "Loans",
                type: "text",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "DueAt",
                table: "Loans",
                type: "text",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "Loans",
                type: "text",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
