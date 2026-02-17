using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRebalancingSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RebalancingSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CashFlowAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExecutionMode = table.Column<string>(type: "text", nullable: false),
                    TotalPeriods = table.Column<int>(type: "integer", nullable: false),
                    CompletedPeriods = table.Column<int>(type: "integer", nullable: false),
                    Interval = table.Column<string>(type: "text", nullable: false),
                    LumpSumPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    NextExecutionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebalancingSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RebalancingScheduleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ActionsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebalancingScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RebalancingScheduleItems_RebalancingSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "RebalancingSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RebalancingScheduleItems_ScheduleId_PeriodNumber",
                table: "RebalancingScheduleItems",
                columns: new[] { "ScheduleId", "PeriodNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RebalancingSchedules_PortfolioId",
                table: "RebalancingSchedules",
                column: "PortfolioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RebalancingScheduleItems");

            migrationBuilder.DropTable(
                name: "RebalancingSchedules");
        }
    }
}
