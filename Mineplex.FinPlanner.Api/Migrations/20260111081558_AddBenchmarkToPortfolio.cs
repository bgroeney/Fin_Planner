using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkToPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BenchmarkAssetId",
                table: "Portfolios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Rationale",
                table: "Decisions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_BenchmarkAssetId",
                table: "Portfolios",
                column: "BenchmarkAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Portfolios_Assets_BenchmarkAssetId",
                table: "Portfolios",
                column: "BenchmarkAssetId",
                principalTable: "Assets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Portfolios_Assets_BenchmarkAssetId",
                table: "Portfolios");

            migrationBuilder.DropIndex(
                name: "IX_Portfolios_BenchmarkAssetId",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "BenchmarkAssetId",
                table: "Portfolios");

            migrationBuilder.AlterColumn<string>(
                name: "Rationale",
                table: "Decisions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
