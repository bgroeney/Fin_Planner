using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormaliseAllocationTo100 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Scale each portfolio's category allocations proportionally so they sum to 100%.
            // Only affects portfolios where categories exist and the current total is not 0 and not already 100.
            migrationBuilder.Sql(@"
                UPDATE ""AssetCategories"" ac
                SET ""TargetPercentage"" = ROUND(ac.""TargetPercentage"" * (100.0 / totals.total_pct), 1)
                FROM (
                    SELECT ""PortfolioId"", SUM(""TargetPercentage"") AS total_pct
                    FROM ""AssetCategories""
                    GROUP BY ""PortfolioId""
                    HAVING SUM(""TargetPercentage"") > 0 AND ABS(SUM(""TargetPercentage"") - 100) > 0.1
                ) totals
                WHERE ac.""PortfolioId"" = totals.""PortfolioId""
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data migration — cannot be reversed automatically
        }
    }
}
