using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTimingRisk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeVarianceEarlyMonths",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "TimeVarianceLateMonths",
                table: "PropertyDeals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeVarianceEarlyMonths",
                table: "PropertyDeals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimeVarianceLateMonths",
                table: "PropertyDeals",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
