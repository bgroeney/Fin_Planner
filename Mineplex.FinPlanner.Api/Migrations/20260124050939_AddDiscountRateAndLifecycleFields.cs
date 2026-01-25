using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountRateAndLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileUploads_Portfolios_PortfolioId",
                table: "FileUploads");

            migrationBuilder.AddColumn<Guid>(
                name: "AcquiredAssetId",
                table: "PropertyDeals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "PropertyDeals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

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

            migrationBuilder.AddColumn<string>(
                name: "YearlyDCFJson",
                table: "DealSimulationResults",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSnapshots_PortfolioId",
                table: "PerformanceSnapshots",
                column: "PortfolioId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileUploads_Portfolios_PortfolioId",
                table: "FileUploads",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceSnapshots_Portfolios_PortfolioId",
                table: "PerformanceSnapshots",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileUploads_Portfolios_PortfolioId",
                table: "FileUploads");

            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceSnapshots_Portfolios_PortfolioId",
                table: "PerformanceSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceSnapshots_PortfolioId",
                table: "PerformanceSnapshots");

            migrationBuilder.DropColumn(
                name: "AcquiredAssetId",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "TimeVarianceEarlyMonths",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "TimeVarianceLateMonths",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "YearlyDCFJson",
                table: "DealSimulationResults");

            migrationBuilder.AddForeignKey(
                name: "FK_FileUploads_Portfolios_PortfolioId",
                table: "FileUploads",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
