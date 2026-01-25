using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedPriceSourceSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PriceSources",
                columns: new[] { "Id", "ApiKey", "Code", "ConfigurationJson", "IsEnabled", "Name", "Priority", "RateLimitPerMinute" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), null, "IMPORTED", null, true, "Imported File", 10, 10000 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));
        }
    }
}
