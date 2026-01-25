using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxParcelsAndReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllocationMethod",
                table: "Decisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectedTaxImpact",
                table: "Decisions",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxParcels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcquisitionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CostBase = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Units = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    RemainingUnits = table.Column<decimal>(type: "numeric(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxParcels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxParcels_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxParcels_AssetId",
                table: "TaxParcels",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxParcels");

            migrationBuilder.DropColumn(
                name: "AllocationMethod",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ProjectedTaxImpact",
                table: "Decisions");
        }
    }
}
