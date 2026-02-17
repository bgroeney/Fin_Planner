using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributionSettingsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationMatrixJson",
                table: "PropertyDeals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistributionSettingsJson",
                table: "PropertyDeals",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrelationMatrixJson",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "DistributionSettingsJson",
                table: "PropertyDeals");
        }
    }
}
