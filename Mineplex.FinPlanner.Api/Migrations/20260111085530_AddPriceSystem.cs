using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrentPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceUsed = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrentPrices_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    ClosePrice = table.Column<decimal>(type: "numeric(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricalPrices_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "AssetPriceSourceOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetPriceSourceOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetPriceSourceOverrides_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetPriceSourceOverrides_PriceSources_PriceSourceId",
                        column: x => x.PriceSourceId,
                        principalTable: "PriceSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PriceSources",
                columns: new[] { "Id", "ApiKey", "Code", "ConfigurationJson", "IsEnabled", "Name", "Priority", "RateLimitPerMinute" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, "YAHOO", null, true, "Yahoo Finance", 1, 2000 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, "ALPHAVANTAGE", null, false, "Alpha Vantage", 2, 5 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), null, "POLYGON", null, false, "Polygon.io", 3, 5 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), null, "TWELVEDATA", null, false, "Twelve Data", 4, 8 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), null, "IEXCLOUD", null, false, "IEX Cloud", 5, 100 }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Key", "Description", "LastModified", "Value" },
                values: new object[] { "PriceUpdateIntervalMinutes", "How often to update market prices (in minutes)", new DateTime(2026, 1, 11, 0, 0, 0, 0, DateTimeKind.Utc), "15" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPriceSourceOverrides_AssetId",
                table: "AssetPriceSourceOverrides",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPriceSourceOverrides_PriceSourceId",
                table: "AssetPriceSourceOverrides",
                column: "PriceSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPrices_AssetId",
                table: "CurrentPrices",
                column: "AssetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPrices_LastUpdated",
                table: "CurrentPrices",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalPrices_AssetId_Date",
                table: "HistoricalPrices",
                columns: new[] { "AssetId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceSources_Code",
                table: "PriceSources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceSources_Priority",
                table: "PriceSources",
                column: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetPriceSourceOverrides");

            migrationBuilder.DropTable(
                name: "CurrentPrices");

            migrationBuilder.DropTable(
                name: "HistoricalPrices");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "PriceSources");
        }
    }
}
