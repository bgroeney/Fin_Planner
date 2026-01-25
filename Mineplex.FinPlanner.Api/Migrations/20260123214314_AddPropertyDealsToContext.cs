using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyDealsToContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyDeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    BuildingType = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AskingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedValue = table.Column<decimal>(type: "numeric", nullable: false),
                    StampDutyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LegalCosts = table.Column<decimal>(type: "numeric", nullable: false),
                    CapExReserve = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedGrossRent = table.Column<decimal>(type: "numeric", nullable: false),
                    VacancyRatePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    ManagementFeePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    OutgoingsEstimate = table.Column<decimal>(type: "numeric", nullable: false),
                    LoanAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestRatePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LoanTermYears = table.Column<int>(type: "integer", nullable: false),
                    RentVariancePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    VacancyVariancePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestVariancePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    CapitalGrowthPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    CapitalGrowthVariancePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    HoldingPeriodYears = table.Column<int>(type: "integer", nullable: false),
                    DecisionRationale = table.Column<string>(type: "text", nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyDeals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealSimulationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Iterations = table.Column<int>(type: "integer", nullable: false),
                    MedianNPV = table.Column<decimal>(type: "numeric", nullable: false),
                    P10NPV = table.Column<decimal>(type: "numeric", nullable: false),
                    P90NPV = table.Column<decimal>(type: "numeric", nullable: false),
                    MedianIRR = table.Column<decimal>(type: "numeric", nullable: false),
                    P10IRR = table.Column<decimal>(type: "numeric", nullable: false),
                    P90IRR = table.Column<decimal>(type: "numeric", nullable: false),
                    CalculatedCapRate = table.Column<decimal>(type: "numeric", nullable: false),
                    RecommendedDecision = table.Column<string>(type: "text", nullable: false),
                    NPVHistogramJson = table.Column<string>(type: "text", nullable: true),
                    IRRHistogramJson = table.Column<string>(type: "text", nullable: true),
                    InputsSnapshotJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealSimulationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealSimulationResults_PropertyDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "PropertyDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DealSimulationResults_DealId",
                table: "DealSimulationResults",
                column: "DealId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealSimulationResults");

            migrationBuilder.DropTable(
                name: "PropertyDeals");
        }
    }
}
