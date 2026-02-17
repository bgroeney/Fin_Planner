using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class RebalancingSchedule
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashFlowAmount { get; set; } // Positive = add funds, negative = withdraw

        public ExecutionMode ExecutionMode { get; set; }
        public int TotalPeriods { get; set; }
        public int CompletedPeriods { get; set; }
        public ScheduleInterval Interval { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal LumpSumPercentage { get; set; } // For Combination mode (e.g. 50 = 50%)

        public DateTime? NextExecutionDate { get; set; }
        public ScheduleStatus Status { get; set; }

        public List<RebalancingScheduleItem> Items { get; set; } = new();
    }

    public class RebalancingScheduleItem
    {
        public Guid Id { get; set; }
        public Guid ScheduleId { get; set; }
        public RebalancingSchedule Schedule { get; set; } = null!;

        public int PeriodNumber { get; set; }
        public DateTime PlannedDate { get; set; }
        public DateTime? ExecutedDate { get; set; }
        public ScheduleItemStatus Status { get; set; }

        [Column(TypeName = "text")]
        public string ActionsJson { get; set; } = string.Empty; // Serialized List<RebalancingActionDto>
    }

    public enum ExecutionMode
    {
        LumpSum,
        Distributed,
        Combination
    }

    public enum ScheduleInterval
    {
        Weekly,
        Fortnightly,
        Monthly
    }

    public enum ScheduleStatus
    {
        Active,
        Completed,
        Cancelled
    }

    public enum ScheduleItemStatus
    {
        Pending,
        Executed,
        Skipped
    }
}
