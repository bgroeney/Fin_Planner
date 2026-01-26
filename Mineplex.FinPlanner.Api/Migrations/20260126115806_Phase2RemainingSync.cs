using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase2RemainingSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Liabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    MonthlyRepayment = table.Column<decimal>(type: "numeric", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TermMonths = table.Column<int>(type: "integer", nullable: false),
                    IsPaidOff = table.Column<bool>(type: "boolean", nullable: false),
                    LastCalculatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemainingBalance = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Liabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RetirementScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RetirementAge = table.Column<int>(type: "integer", nullable: false),
                    TargetAnnualIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedInflationRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedReturnRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Volatility = table.Column<decimal>(type: "numeric", nullable: false),
                    IncludeAgePension = table.Column<bool>(type: "boolean", nullable: false),
                    RetirementExpenses = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetirementScenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LifeEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetirementScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    YearOffset = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    RecurringFrequencyYears = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LifeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LifeEvents_RetirementScenarios_RetirementScenarioId",
                        column: x => x.RetirementScenarioId,
                        principalTable: "RetirementScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Liabilities_PortfolioId",
                table: "Liabilities",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_LifeEvents_RetirementScenarioId",
                table: "LifeEvents",
                column: "RetirementScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RetirementScenarios_PortfolioId",
                table: "RetirementScenarios",
                column: "PortfolioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Liabilities");

            migrationBuilder.DropTable(
                name: "LifeEvents");

            migrationBuilder.DropTable(
                name: "RetirementScenarios");
        }
    }
}
