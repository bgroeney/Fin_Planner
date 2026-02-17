using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSharesightSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PriceSources",
                columns: new[] { "Id", "ApiKey", "Code", "ConfigurationJson", "IsEnabled", "Name", "Priority", "RateLimitPerMinute" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), null, "SHARESIGHT_AU", null, true, "Sharesight Australia", 5, 60 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));
        }
    }
}
