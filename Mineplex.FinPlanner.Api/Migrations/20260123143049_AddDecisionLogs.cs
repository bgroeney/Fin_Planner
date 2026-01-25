using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecisionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionLogs_DecisionId",
                table: "DecisionLogs",
                column: "DecisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecisionLogs");
        }
    }
}
