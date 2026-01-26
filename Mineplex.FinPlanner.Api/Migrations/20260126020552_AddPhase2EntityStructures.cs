using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mineplex.FinPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2EntityStructures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    ABN = table.Column<string>(type: "text", nullable: true),
                    ACN = table.Column<string>(type: "text", nullable: true),
                    RetainedProfits = table.Column<decimal>(type: "numeric", nullable: false),
                    FrankingAccountBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    IsBaseRateEntity = table.Column<bool>(type: "boolean", nullable: false),
                    IncorporationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyAccounts_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TaxFileNumber = table.Column<string>(type: "text", nullable: true),
                    MarginalTaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    MedicareLevyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    HasPrivateHealthInsurance = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonAccounts_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrustAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrustName = table.Column<string>(type: "text", nullable: false),
                    ABN = table.Column<string>(type: "text", nullable: true),
                    TrusteeName = table.Column<string>(type: "text", nullable: false),
                    TrustType = table.Column<string>(type: "text", nullable: false),
                    TrustDeedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MustDistributeByYearEnd = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustAccounts_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDividends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeclarationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    FrankingPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    FrankingCredits = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDividends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyDividends_CompanyAccounts_CompanyAccountId",
                        column: x => x.CompanyAccountId,
                        principalTable: "CompanyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsEstimate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deductions_PersonAccounts_PersonAccountId",
                        column: x => x.PersonAccountId,
                        principalTable: "PersonAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Division7ALoans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BorrowerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BorrowerName = table.Column<string>(type: "text", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LoanDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoanTermYears = table.Column<int>(type: "integer", nullable: false),
                    IsSecured = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumYearlyRepayment = table.Column<decimal>(type: "numeric", nullable: false),
                    RepaidThisYear = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Division7ALoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Division7ALoans_CompanyAccounts_CompanyAccountId",
                        column: x => x.CompanyAccountId,
                        principalTable: "CompanyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Division7ALoans_PersonAccounts_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "PersonAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollIncomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    Employer = table.Column<string>(type: "text", nullable: false),
                    GrossSalary = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxWithheld = table.Column<decimal>(type: "numeric", nullable: false),
                    SuperContribution = table.Column<decimal>(type: "numeric", nullable: false),
                    SalarySacrifice = table.Column<decimal>(type: "numeric", nullable: false),
                    ReportableFringeBenefits = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollIncomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollIncomes_PersonAccounts_PersonAccountId",
                        column: x => x.PersonAccountId,
                        principalTable: "PersonAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuperAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FundName = table.Column<string>(type: "text", nullable: false),
                    MemberNumber = table.Column<string>(type: "text", nullable: true),
                    CurrentBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    InvestmentOption = table.Column<string>(type: "text", nullable: false),
                    PreservationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployerContributionYTD = table.Column<decimal>(type: "numeric", nullable: false),
                    SalarySacrificeYTD = table.Column<decimal>(type: "numeric", nullable: false),
                    PersonalContributionYTD = table.Column<decimal>(type: "numeric", nullable: false),
                    ConcessionalCapRemaining = table.Column<decimal>(type: "numeric", nullable: false),
                    NonConcessionalCapRemaining = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuperAccounts_PersonAccounts_PersonAccountId",
                        column: x => x.PersonAccountId,
                        principalTable: "PersonAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrustBeneficiaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrustAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    BeneficiaryName = table.Column<string>(type: "text", nullable: false),
                    IsEligible = table.Column<bool>(type: "boolean", nullable: false),
                    Relationship = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustBeneficiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustBeneficiaries_CompanyAccounts_CompanyAccountId",
                        column: x => x.CompanyAccountId,
                        principalTable: "CompanyAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrustBeneficiaries_PersonAccounts_PersonAccountId",
                        column: x => x.PersonAccountId,
                        principalTable: "PersonAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrustBeneficiaries_TrustAccounts_TrustAccountId",
                        column: x => x.TrustAccountId,
                        principalTable: "TrustAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrustIncomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrustAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    FrankedDividends = table.Column<decimal>(type: "numeric", nullable: false),
                    UnfrankedIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountCapitalGains = table.Column<decimal>(type: "numeric", nullable: false),
                    NonDiscountCapitalGains = table.Column<decimal>(type: "numeric", nullable: false),
                    RentalIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    FrankingCredits = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustIncomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustIncomes_TrustAccounts_TrustAccountId",
                        column: x => x.TrustAccountId,
                        principalTable: "TrustAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrustDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrustAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    ResolutionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FrankedDividends = table.Column<decimal>(type: "numeric", nullable: false),
                    UnfrankedIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountCapitalGains = table.Column<decimal>(type: "numeric", nullable: false),
                    NonDiscountCapitalGains = table.Column<decimal>(type: "numeric", nullable: false),
                    FrankingCredits = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustDistributions_TrustAccounts_TrustAccountId",
                        column: x => x.TrustAccountId,
                        principalTable: "TrustAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrustDistributions_TrustBeneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "TrustBeneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAccounts_PortfolioId",
                table: "CompanyAccounts",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDividends_CompanyAccountId",
                table: "CompanyDividends",
                column: "CompanyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Deductions_PersonAccountId",
                table: "Deductions",
                column: "PersonAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Division7ALoans_BorrowerId",
                table: "Division7ALoans",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_Division7ALoans_CompanyAccountId",
                table: "Division7ALoans",
                column: "CompanyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollIncomes_PersonAccountId",
                table: "PayrollIncomes",
                column: "PersonAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonAccounts_PortfolioId",
                table: "PersonAccounts",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_SuperAccounts_PersonAccountId",
                table: "SuperAccounts",
                column: "PersonAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustAccounts_PortfolioId",
                table: "TrustAccounts",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustBeneficiaries_CompanyAccountId",
                table: "TrustBeneficiaries",
                column: "CompanyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustBeneficiaries_PersonAccountId",
                table: "TrustBeneficiaries",
                column: "PersonAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustBeneficiaries_TrustAccountId_PersonAccountId",
                table: "TrustBeneficiaries",
                columns: new[] { "TrustAccountId", "PersonAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrustDistributions_BeneficiaryId",
                table: "TrustDistributions",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustDistributions_TrustAccountId_FiscalYear",
                table: "TrustDistributions",
                columns: new[] { "TrustAccountId", "FiscalYear" });

            migrationBuilder.CreateIndex(
                name: "IX_TrustIncomes_TrustAccountId",
                table: "TrustIncomes",
                column: "TrustAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyDividends");

            migrationBuilder.DropTable(
                name: "Deductions");

            migrationBuilder.DropTable(
                name: "Division7ALoans");

            migrationBuilder.DropTable(
                name: "PayrollIncomes");

            migrationBuilder.DropTable(
                name: "SuperAccounts");

            migrationBuilder.DropTable(
                name: "TrustDistributions");

            migrationBuilder.DropTable(
                name: "TrustIncomes");

            migrationBuilder.DropTable(
                name: "TrustBeneficiaries");

            migrationBuilder.DropTable(
                name: "CompanyAccounts");

            migrationBuilder.DropTable(
                name: "PersonAccounts");

            migrationBuilder.DropTable(
                name: "TrustAccounts");
        }
    }
}
