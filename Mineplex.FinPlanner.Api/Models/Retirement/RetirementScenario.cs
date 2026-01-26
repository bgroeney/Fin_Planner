using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models.Retirement
{
    public class RetirementScenario
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public required string Name { get; set; }

        public int RetirementAge { get; set; } = 65;
        public decimal TargetAnnualIncome { get; set; }
        public decimal ExpectedInflationRate { get; set; } = 0.025m;
        public decimal ExpectedReturnRate { get; set; } = 0.07m;
        public decimal Volatility { get; set; } = 0.15m;

        public bool IncludeAgePension { get; set; } = true;
        public decimal RetirementExpenses { get; set; }

        public List<LifeEvent> LifeEvents { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class LifeEvent
    {
        public Guid Id { get; set; }
        public Guid RetirementScenarioId { get; set; }
        public required string Description { get; set; }
        public int YearOffset { get; set; } // Years from now
        public decimal Amount { get; set; } // Positive for income, negative for expense
        public bool IsRecurring { get; set; }
        public int? RecurringFrequencyYears { get; set; }
    }
}
