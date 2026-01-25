using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRetiredPriceSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PriceSources",
                columns: new[] { "Id", "ApiKey", "Code", "ConfigurationJson", "IsEnabled", "Name", "Priority", "RateLimitPerMinute" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), null, "TWELVEDATA", null, false, "Twelve Data", 4, 8 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), null, "IEXCLOUD", null, false, "IEX Cloud", 5, 100 }
                });
        }
    }
}
