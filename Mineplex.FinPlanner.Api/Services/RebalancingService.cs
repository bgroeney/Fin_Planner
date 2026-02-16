using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IRebalancingService
    {
        Task<RebalancingReportDto> GetRebalancingReportAsync(Guid portfolioId, decimal cashFlowAmount = 0);
        Task<List<Guid>> ExecuteRebalancingAsync(Guid portfolioId, List<RebalancingActionDto> actions, Guid userId, string userEmail);
        Task<ScheduleResultDto> ExecuteScheduledRebalancingAsync(Guid portfolioId, ScheduledRebalancingRequestDto request, Guid userId, string userEmail);
        Task<List<RebalancingScheduleDto>> GetActiveSchedulesAsync(Guid portfolioId);
        Task<TrancheExecutionResultDto> ExecuteNextTrancheAsync(Guid scheduleId, Guid userId, string userEmail);
        Task CancelScheduleAsync(Guid scheduleId, Guid userId, string userEmail);
    }

    public class RebalancingService : IRebalancingService
    {
        private readonly FinPlannerDbContext _context;
        private readonly IAuditService _auditService;

        public RebalancingService(FinPlannerDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<RebalancingReportDto> GetRebalancingReportAsync(Guid portfolioId, decimal cashFlowAmount = 0)
        {
            var categories = await _context.AssetCategories
                .Where(c => c.PortfolioId == portfolioId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var holdings = await _context.Holdings
                .Include(h => h.Asset)
                    .ThenInclude(a => a.CurrentPrice)
                .Include(h => h.Account)
                .Where(h => h.Account.PortfolioId == portfolioId)
                .ToListAsync();

            var currentPortfolioValue = holdings.Sum(h => h.Units * (h.Asset.CurrentPrice?.Price ?? 0));
            var adjustedPortfolioValue = currentPortfolioValue + cashFlowAmount;

            // Ensure we don't go negative
            if (adjustedPortfolioValue < 0)
                adjustedPortfolioValue = 0;

            var report = new RebalancingReportDto
            {
                PortfolioId = portfolioId,
                TotalValue = currentPortfolioValue,
                AdjustedTotalValue = adjustedPortfolioValue,
                CashFlowAmount = cashFlowAmount,
                Categories = new List<CategoryRebalanceDto>()
            };

            foreach (var category in categories)
            {
                var categoryHoldings = holdings.Where(h => h.CategoryId == category.Id).ToList();
                var currentValue = categoryHoldings.Sum(h => h.Units * (h.Asset.CurrentPrice?.Price ?? 0));
                var currentPercentage = currentPortfolioValue > 0 ? (currentValue / currentPortfolioValue) * 100 : 0;

                // Target value is calculated against the ADJUSTED total (with cash flow)
                var targetValue = (category.TargetPercentage / 100) * adjustedPortfolioValue;
                var variance = currentValue - targetValue;

                var categoryDto = new CategoryRebalanceDto
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    TargetPercentage = category.TargetPercentage,
                    CurrentPercentage = currentPercentage,
                    CurrentValue = currentValue,
                    TargetValue = targetValue,
                    VarianceAmount = variance,
                    Recommendation = variance > 0 ? "Sell" : (variance < 0 ? "Buy" : "Hold"),
                    Assets = categoryHoldings.Select(h => new AssetHoldingsDto
                    {
                        AssetId = h.AssetId,
                        Symbol = h.Asset.Symbol,
                        Name = h.Asset.Name,
                        Units = h.Units,
                        CurrentPrice = h.Asset.CurrentPrice?.Price ?? 0,
                        TotalValue = h.Units * (h.Asset.CurrentPrice?.Price ?? 0),
                        WeightInCategory = currentValue > 0 ? (h.Units * (h.Asset.CurrentPrice?.Price ?? 0) / currentValue) * 100 : 0
                    }).ToList()
                };

                report.Categories.Add(categoryDto);
            }

            return report;
        }

        public async Task<List<Guid>> ExecuteRebalancingAsync(Guid portfolioId, List<RebalancingActionDto> actions, Guid userId, string userEmail)
        {
            var createdTransactionIds = new List<Guid>();

            foreach (var action in actions)
            {
                var existingHolding = await _context.Holdings
                    .FirstOrDefaultAsync(h => h.AssetId == action.AssetId && h.Account.PortfolioId == portfolioId);

                Guid accountId;
                if (existingHolding != null)
                {
                    accountId = existingHolding.AccountId;
                }
                else
                {
                    var firstAccount = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.PortfolioId == portfolioId);
                    if (firstAccount == null) continue;
                    accountId = firstAccount.Id;
                }

                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    AssetId = action.AssetId,
                    Type = action.Type,
                    Units = action.Units,
                    Amount = action.Amount,
                    EffectiveDate = DateTime.UtcNow,
                    Narration = $"Rebalancing: {action.Type} {action.Units:F4} units based on target allocation"
                };

                _context.Transactions.Add(transaction);
                createdTransactionIds.Add(transaction.Id);

                if (existingHolding != null)
                {
                    if (action.Type == TransactionType.Buy || action.Type == TransactionType.Deposit)
                    {
                        var newTotalCost = (existingHolding.Units * existingHolding.AvgCost) + Math.Abs(action.Amount);
                        existingHolding.Units += action.Units;
                        existingHolding.AvgCost = existingHolding.Units > 0 ? newTotalCost / existingHolding.Units : 0;
                    }
                    else if (action.Type == TransactionType.Sell || action.Type == TransactionType.Withdrawal)
                    {
                        existingHolding.Units -= action.Units;
                    }
                }
                else if (action.Type == TransactionType.Buy)
                {
                    var holding = new Holding
                    {
                        Id = Guid.NewGuid(),
                        AccountId = accountId,
                        AssetId = action.AssetId,
                        Units = action.Units,
                        AvgCost = action.Units > 0 ? Math.Abs(action.Amount) / action.Units : 0,
                        CategoryId = (await _context.AssetCategories.FirstOrDefaultAsync(c => c.PortfolioId == portfolioId))?.Id ?? Guid.Empty
                    };
                    _context.Holdings.Add(holding);
                }

                await _auditService.LogAsync(userId, userEmail, "ExecuteRebalancing", "Portfolio", portfolioId, $"Rebalanced {action.AssetId}");
            }

            await _context.SaveChangesAsync();
            return createdTransactionIds;
        }

        public async Task<ScheduleResultDto> ExecuteScheduledRebalancingAsync(
            Guid portfolioId, ScheduledRebalancingRequestDto request, Guid userId, string userEmail)
        {
            var result = new ScheduleResultDto();

            if (request.ExecutionMode == ExecutionMode.LumpSum)
            {
                // Execute everything immediately
                var transactionIds = await ExecuteRebalancingAsync(portfolioId, request.Actions, userId, userEmail);
                result.ExecutedTransactionIds = transactionIds;
                result.Message = $"Executed {transactionIds.Count} trades immediately.";
                return result;
            }

            // Create the schedule
            var schedule = new RebalancingSchedule
            {
                Id = Guid.NewGuid(),
                PortfolioId = portfolioId,
                CreatedByUserId = userId,
                CreatedDate = DateTime.UtcNow,
                CashFlowAmount = request.CashFlowAmount,
                ExecutionMode = request.ExecutionMode,
                TotalPeriods = request.TotalPeriods,
                CompletedPeriods = 0,
                Interval = request.Interval,
                LumpSumPercentage = request.LumpSumPercentage,
                Status = ScheduleStatus.Active
            };

            // Calculate tranches
            var lumpPct = request.ExecutionMode == ExecutionMode.Combination ? request.LumpSumPercentage / 100m : 0m;
            var distributedPct = 1m - lumpPct;
            var distributedPeriods = request.TotalPeriods;

            // For combination mode, period 0 is the lump sum tranche
            int periodStart = 0;
            if (request.ExecutionMode == ExecutionMode.Combination)
            {
                // Create lump sum tranche (period 0)
                var lumpActions = request.Actions.Select(a => new RebalancingActionDto
                {
                    AssetId = a.AssetId,
                    Type = a.Type,
                    Units = Math.Round(a.Units * lumpPct, 6),
                    Amount = Math.Round(a.Amount * lumpPct, 2)
                }).ToList();

                schedule.Items.Add(new RebalancingScheduleItem
                {
                    Id = Guid.NewGuid(),
                    ScheduleId = schedule.Id,
                    PeriodNumber = 0,
                    PlannedDate = DateTime.UtcNow,
                    Status = ScheduleItemStatus.Pending,
                    ActionsJson = JsonSerializer.Serialize(lumpActions)
                });

                periodStart = 1;
            }

            // Create distributed tranches
            var perPeriodFraction = distributedPct / distributedPeriods;
            for (int i = 0; i < distributedPeriods; i++)
            {
                var periodNumber = periodStart + i;
                var plannedDate = CalculatePlannedDate(DateTime.UtcNow, i + (request.ExecutionMode == ExecutionMode.Combination ? 1 : 0), request.Interval);

                var trancheActions = request.Actions.Select(a => new RebalancingActionDto
                {
                    AssetId = a.AssetId,
                    Type = a.Type,
                    Units = Math.Round(a.Units * perPeriodFraction, 6),
                    Amount = Math.Round(a.Amount * perPeriodFraction, 2)
                }).ToList();

                schedule.Items.Add(new RebalancingScheduleItem
                {
                    Id = Guid.NewGuid(),
                    ScheduleId = schedule.Id,
                    PeriodNumber = periodNumber,
                    PlannedDate = plannedDate,
                    Status = ScheduleItemStatus.Pending,
                    ActionsJson = JsonSerializer.Serialize(trancheActions)
                });
            }

            // Set next execution date
            schedule.NextExecutionDate = schedule.Items.OrderBy(i => i.PlannedDate).First().PlannedDate;

            _context.RebalancingSchedules.Add(schedule);

            // Execute the first tranche immediately (period 0 for combo, or period 0 for distributed if it's today)
            var firstItem = schedule.Items.OrderBy(i => i.PeriodNumber).First();
            if (firstItem.PlannedDate.Date <= DateTime.UtcNow.Date)
            {
                var firstActions = JsonSerializer.Deserialize<List<RebalancingActionDto>>(firstItem.ActionsJson) ?? new();
                var txIds = await ExecuteRebalancingAsync(portfolioId, firstActions, userId, userEmail);
                firstItem.Status = ScheduleItemStatus.Executed;
                firstItem.ExecutedDate = DateTime.UtcNow;
                schedule.CompletedPeriods = 1;
                schedule.NextExecutionDate = schedule.Items
                    .Where(i => i.Status == ScheduleItemStatus.Pending)
                    .OrderBy(i => i.PlannedDate)
                    .FirstOrDefault()?.PlannedDate;

                result.ExecutedTransactionIds = txIds;
            }

            // Check if all done
            if (!schedule.Items.Any(i => i.Status == ScheduleItemStatus.Pending))
            {
                schedule.Status = ScheduleStatus.Completed;
            }

            await _context.SaveChangesAsync();

            result.ScheduleId = schedule.Id;
            result.Message = request.ExecutionMode == ExecutionMode.Combination
                ? $"Executed {request.LumpSumPercentage}% lump sum. Remaining distributed over {request.TotalPeriods} periods."
                : $"Schedule created with {request.TotalPeriods} periods ({request.Interval}).";

            return result;
        }

        public async Task<List<RebalancingScheduleDto>> GetActiveSchedulesAsync(Guid portfolioId)
        {
            var schedules = await _context.RebalancingSchedules
                .Include(s => s.Items)
                .Where(s => s.PortfolioId == portfolioId && s.Status == ScheduleStatus.Active)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            return schedules.Select(s => new RebalancingScheduleDto
            {
                Id = s.Id,
                CreatedDate = s.CreatedDate,
                CashFlowAmount = s.CashFlowAmount,
                ExecutionMode = s.ExecutionMode.ToString(),
                TotalPeriods = s.TotalPeriods,
                CompletedPeriods = s.CompletedPeriods,
                Interval = s.Interval.ToString(),
                LumpSumPercentage = s.LumpSumPercentage,
                NextExecutionDate = s.NextExecutionDate,
                Status = s.Status.ToString(),
                Items = s.Items.OrderBy(i => i.PeriodNumber).Select(i => new RebalancingScheduleItemDto
                {
                    Id = i.Id,
                    PeriodNumber = i.PeriodNumber,
                    PlannedDate = i.PlannedDate,
                    ExecutedDate = i.ExecutedDate,
                    Status = i.Status.ToString(),
                    Actions = JsonSerializer.Deserialize<List<RebalancingActionDto>>(i.ActionsJson) ?? new()
                }).ToList()
            }).ToList();
        }

        public async Task<TrancheExecutionResultDto> ExecuteNextTrancheAsync(Guid scheduleId, Guid userId, string userEmail)
        {
            var schedule = await _context.RebalancingSchedules
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Status == ScheduleStatus.Active);

            if (schedule == null)
                throw new InvalidOperationException("Schedule not found or already completed.");

            var nextItem = schedule.Items
                .Where(i => i.Status == ScheduleItemStatus.Pending)
                .OrderBy(i => i.PeriodNumber)
                .FirstOrDefault();

            if (nextItem == null)
                throw new InvalidOperationException("No pending tranches remaining.");

            var actions = JsonSerializer.Deserialize<List<RebalancingActionDto>>(nextItem.ActionsJson) ?? new();
            var txIds = await ExecuteRebalancingAsync(schedule.PortfolioId, actions, userId, userEmail);

            nextItem.Status = ScheduleItemStatus.Executed;
            nextItem.ExecutedDate = DateTime.UtcNow;
            schedule.CompletedPeriods++;

            // Update next execution date
            var remaining = schedule.Items
                .Where(i => i.Status == ScheduleItemStatus.Pending)
                .OrderBy(i => i.PlannedDate)
                .FirstOrDefault();

            schedule.NextExecutionDate = remaining?.PlannedDate;

            if (remaining == null)
            {
                schedule.Status = ScheduleStatus.Completed;
            }

            await _context.SaveChangesAsync();

            return new TrancheExecutionResultDto
            {
                TransactionIds = txIds,
                PeriodNumber = nextItem.PeriodNumber,
                RemainingPeriods = schedule.Items.Count(i => i.Status == ScheduleItemStatus.Pending),
                ScheduleCompleted = schedule.Status == ScheduleStatus.Completed
            };
        }

        public async Task CancelScheduleAsync(Guid scheduleId, Guid userId, string userEmail)
        {
            var schedule = await _context.RebalancingSchedules
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Status == ScheduleStatus.Active);

            if (schedule == null)
                throw new InvalidOperationException("Schedule not found or already completed.");

            schedule.Status = ScheduleStatus.Cancelled;
            foreach (var item in schedule.Items.Where(i => i.Status == ScheduleItemStatus.Pending))
            {
                item.Status = ScheduleItemStatus.Skipped;
            }

            await _auditService.LogAsync(userId, userEmail, "CancelRebalancingSchedule", "RebalancingSchedule", scheduleId, "Schedule cancelled");
            await _context.SaveChangesAsync();
        }

        private DateTime CalculatePlannedDate(DateTime from, int periodsAhead, ScheduleInterval interval)
        {
            return interval switch
            {
                ScheduleInterval.Weekly => from.AddDays(7 * periodsAhead),
                ScheduleInterval.Fortnightly => from.AddDays(14 * periodsAhead),
                ScheduleInterval.Monthly => from.AddMonths(periodsAhead),
                _ => from.AddMonths(periodsAhead)
            };
        }
    }

    // DTOs
    public class RebalancingReportDto
    {
        public Guid PortfolioId { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AdjustedTotalValue { get; set; }
        public decimal CashFlowAmount { get; set; }
        public List<CategoryRebalanceDto> Categories { get; set; } = new();
    }

    public class CategoryRebalanceDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal TargetPercentage { get; set; }
        public decimal CurrentPercentage { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal TargetValue { get; set; }
        public decimal VarianceAmount { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public List<AssetHoldingsDto> Assets { get; set; } = new();
    }

    public class AssetHoldingsDto
    {
        public Guid AssetId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Units { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalValue { get; set; }
        public decimal WeightInCategory { get; set; }
    }

    public class RebalancingActionDto
    {
        public Guid AssetId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Units { get; set; }
        public decimal Amount { get; set; }
    }

    // New DTOs for scheduling
    public class ScheduledRebalancingRequestDto
    {
        public List<RebalancingActionDto> Actions { get; set; } = new();
        public ExecutionMode ExecutionMode { get; set; }
        public int TotalPeriods { get; set; } = 1;
        public ScheduleInterval Interval { get; set; } = ScheduleInterval.Monthly;
        public decimal LumpSumPercentage { get; set; } = 50; // For combination mode
        public decimal CashFlowAmount { get; set; }
    }

    public class ScheduleResultDto
    {
        public Guid? ScheduleId { get; set; }
        public List<Guid> ExecutedTransactionIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class RebalancingScheduleDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal CashFlowAmount { get; set; }
        public string ExecutionMode { get; set; } = string.Empty;
        public int TotalPeriods { get; set; }
        public int CompletedPeriods { get; set; }
        public string Interval { get; set; } = string.Empty;
        public decimal LumpSumPercentage { get; set; }
        public DateTime? NextExecutionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<RebalancingScheduleItemDto> Items { get; set; } = new();
    }

    public class RebalancingScheduleItemDto
    {
        public Guid Id { get; set; }
        public int PeriodNumber { get; set; }
        public DateTime PlannedDate { get; set; }
        public DateTime? ExecutedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<RebalancingActionDto> Actions { get; set; } = new();
    }

    public class TrancheExecutionResultDto
    {
        public List<Guid> TransactionIds { get; set; } = new();
        public int PeriodNumber { get; set; }
        public int RemainingPeriods { get; set; }
        public bool ScheduleCompleted { get; set; }
    }
}
