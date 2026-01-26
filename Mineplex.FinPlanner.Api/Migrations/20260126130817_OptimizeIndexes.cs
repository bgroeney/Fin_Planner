using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceSnapshots_PortfolioId",
                table: "PerformanceSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_EffectiveDate",
                table: "Transactions",
                columns: new[] { "AccountId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSnapshots_PortfolioId_Date",
                table: "PerformanceSnapshots",
                columns: new[] { "PortfolioId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_EffectiveDate",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceSnapshots_PortfolioId_Date",
                table: "PerformanceSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSnapshots_PortfolioId",
                table: "PerformanceSnapshots",
                column: "PortfolioId");
        }
    }
}
