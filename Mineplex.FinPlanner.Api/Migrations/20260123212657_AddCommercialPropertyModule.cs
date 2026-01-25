using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialPropertyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommercialProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    TitleReference = table.Column<string>(type: "text", nullable: true),
                    TotalGFA = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BuildingType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProxyAssetId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommercialProperties_Assets_ProxyAssetId",
                        column: x => x.ProxyAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommercialProperties_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaseProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantName = table.Column<string>(type: "text", nullable: false),
                    UnitReference = table.Column<string>(type: "text", nullable: true),
                    LeaseStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OptionPeriod = table.Column<string>(type: "text", nullable: true),
                    CurrentRent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReviewType = table.Column<string>(type: "text", nullable: false),
                    ReviewPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    OutgoingsRecoverable = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    GLA = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaseProfiles_CommercialProperties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "CommercialProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsIncome = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FileUploadId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyLedgerEntries_CommercialProperties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "CommercialProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropertyLedgerEntries_FileUploads_FileUploadId",
                        column: x => x.FileUploadId,
                        principalTable: "FileUploads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PropertyValuations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyValuations_CommercialProperties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "CommercialProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommercialProperties_OwnerId",
                table: "CommercialProperties",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialProperties_ProxyAssetId",
                table: "CommercialProperties",
                column: "ProxyAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseProfiles_LeaseEnd",
                table: "LeaseProfiles",
                column: "LeaseEnd");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseProfiles_PropertyId",
                table: "LeaseProfiles",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyLedgerEntries_FileUploadId",
                table: "PropertyLedgerEntries",
                column: "FileUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyLedgerEntries_PropertyId_Date",
                table: "PropertyLedgerEntries",
                columns: new[] { "PropertyId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyValuations_PropertyId_Date",
                table: "PropertyValuations",
                columns: new[] { "PropertyId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaseProfiles");

            migrationBuilder.DropTable(
                name: "PropertyLedgerEntries");

            migrationBuilder.DropTable(
                name: "PropertyValuations");

            migrationBuilder.DropTable(
                name: "CommercialProperties");
        }
    }
}
