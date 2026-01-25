using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestedSymbolToOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetPriceSourceOverrides_PriceSources_PriceSourceId",
                table: "AssetPriceSourceOverrides");

            migrationBuilder.AlterColumn<Guid>(
                name: "PriceSourceId",
                table: "AssetPriceSourceOverrides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "SuggestedSymbol",
                table: "AssetPriceSourceOverrides",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetPriceSourceOverrides_PriceSources_PriceSourceId",
                table: "AssetPriceSourceOverrides",
                column: "PriceSourceId",
                principalTable: "PriceSources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetPriceSourceOverrides_PriceSources_PriceSourceId",
                table: "AssetPriceSourceOverrides");

            migrationBuilder.DropColumn(
                name: "SuggestedSymbol",
                table: "AssetPriceSourceOverrides");

            migrationBuilder.AlterColumn<Guid>(
                name: "PriceSourceId",
                table: "AssetPriceSourceOverrides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetPriceSourceOverrides_PriceSources_PriceSourceId",
                table: "AssetPriceSourceOverrides",
                column: "PriceSourceId",
                principalTable: "PriceSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
