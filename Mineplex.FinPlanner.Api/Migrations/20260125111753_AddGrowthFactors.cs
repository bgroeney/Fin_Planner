using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGrowthFactors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ManagementGrowthPercent",
                table: "PropertyDeals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OutgoingsGrowthPercent",
                table: "PropertyDeals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VacancyGrowthPercent",
                table: "PropertyDeals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DealStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<string>(type: "text", nullable: false),
                    NewStatus = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealStatusHistory_PropertyDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "PropertyDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DealStatusHistory_DealId",
                table: "DealStatusHistory",
                column: "DealId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealStatusHistory");

            migrationBuilder.DropColumn(
                name: "ManagementGrowthPercent",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "OutgoingsGrowthPercent",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "VacancyGrowthPercent",
                table: "PropertyDeals");
        }
    }
}
