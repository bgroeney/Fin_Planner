using System;
using System.ComponentModel.DataAnnotations;

namespace Mineplex.FinPlanner.Api.Models.Retirement
{
    public class Liability
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public required string Name { get; set; }
        public string Type { get; set; } = "Mortgage"; // Mortgage, Personal Loan, Credit Card, etc.
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public decimal MonthlyRepayment { get; set; }
        public DateTime StartDate { get; set; }
        public int TermMonths { get; set; }

        public bool IsPaidOff { get; set; }
        public DateTime? LastCalculatedDate { get; set; }
        public decimal? RemainingBalance { get; set; }
    }
}
