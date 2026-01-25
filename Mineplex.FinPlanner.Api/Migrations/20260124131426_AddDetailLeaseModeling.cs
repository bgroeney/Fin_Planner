using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailLeaseModeling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LeaseDetailsJson",
                table: "PropertyDeals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoanDetailsJson",
                table: "PropertyDeals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PropertyDealId",
                table: "FileUploads",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "FileUploads",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_PropertyDealId",
                table: "FileUploads",
                column: "PropertyDealId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileUploads_PropertyDeals_PropertyDealId",
                table: "FileUploads",
                column: "PropertyDealId",
                principalTable: "PropertyDeals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileUploads_PropertyDeals_PropertyDealId",
                table: "FileUploads");

            migrationBuilder.DropIndex(
                name: "IX_FileUploads_PropertyDealId",
                table: "FileUploads");

            migrationBuilder.DropColumn(
                name: "LeaseDetailsJson",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "LoanDetailsJson",
                table: "PropertyDeals");

            migrationBuilder.DropColumn(
                name: "PropertyDealId",
                table: "FileUploads");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "FileUploads");
        }
    }
}
