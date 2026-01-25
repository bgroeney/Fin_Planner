using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMorningstarAuPriceSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Priority",
                value: 3);

            migrationBuilder.UpdateData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Priority",
                value: 4);

            migrationBuilder.InsertData(
                table: "PriceSources",
                columns: new[] { "Id", "ApiKey", "Code", "ConfigurationJson", "IsEnabled", "Name", "Priority", "RateLimitPerMinute" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), null, "MORNINGSTAR_AU", null, true, "Morningstar Australia", 2, 120 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.UpdateData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Priority",
                value: 2);

            migrationBuilder.UpdateData(
                table: "PriceSources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Priority",
                value: 3);
        }
    }
}
