using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFileHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Decisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImplementedAt",
                table: "Decisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Decisions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FileUploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValuationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RecordsProcessed = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileUploads_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileUploads_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileUploads_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ImportedAssetPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Units = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValuationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedAssetPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedAssetPrices_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedAssetPrices_FileUploads_FileUploadId",
                        column: x => x.FileUploadId,
                        principalTable: "FileUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_AccountId",
                table: "FileUploads",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_PortfolioId",
                table: "FileUploads",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_UploadedByUserId",
                table: "FileUploads",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedAssetPrices_AssetId_ValuationDate",
                table: "ImportedAssetPrices",
                columns: new[] { "AssetId", "ValuationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedAssetPrices_FileUploadId",
                table: "ImportedAssetPrices",
                column: "FileUploadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedAssetPrices");

            migrationBuilder.DropTable(
                name: "FileUploads");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ImplementedAt",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Decisions");
        }
    }
}
