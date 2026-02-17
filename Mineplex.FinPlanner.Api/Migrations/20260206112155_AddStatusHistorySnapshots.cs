using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusHistorySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputsSnapshotJson",
                table: "DealStatusHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SimulationSnapshotId",
                table: "DealStatusHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpreadsheetSnapshotJson",
                table: "DealStatusHistory",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DealStatusHistory_SimulationSnapshotId",
                table: "DealStatusHistory",
                column: "SimulationSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_DealStatusHistory_DealSimulationResults_SimulationSnapshotId",
                table: "DealStatusHistory",
                column: "SimulationSnapshotId",
                principalTable: "DealSimulationResults",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DealStatusHistory_DealSimulationResults_SimulationSnapshotId",
                table: "DealStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_DealStatusHistory_SimulationSnapshotId",
                table: "DealStatusHistory");

            migrationBuilder.DropColumn(
                name: "InputsSnapshotJson",
                table: "DealStatusHistory");

            migrationBuilder.DropColumn(
                name: "SimulationSnapshotId",
                table: "DealStatusHistory");

            migrationBuilder.DropColumn(
                name: "SpreadsheetSnapshotJson",
                table: "DealStatusHistory");
        }
    }
}
