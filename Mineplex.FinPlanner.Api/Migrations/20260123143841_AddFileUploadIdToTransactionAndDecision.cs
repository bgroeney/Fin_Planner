using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFileUploadIdToTransactionAndDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FileUploadId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FileUploadId",
                table: "Decisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FileUploadId",
                table: "Transactions",
                column: "FileUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_FileUploadId",
                table: "Decisions",
                column: "FileUploadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_FileUploads_FileUploadId",
                table: "Decisions",
                column: "FileUploadId",
                principalTable: "FileUploads",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_FileUploads_FileUploadId",
                table: "Transactions",
                column: "FileUploadId",
                principalTable: "FileUploads",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_FileUploads_FileUploadId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_FileUploads_FileUploadId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FileUploadId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_FileUploadId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "FileUploadId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FileUploadId",
                table: "Decisions");
        }
    }
}
