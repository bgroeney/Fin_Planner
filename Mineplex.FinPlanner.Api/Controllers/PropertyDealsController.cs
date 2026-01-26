using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyDealsController : ControllerBase
    {
        private readonly FinPlannerDbContext _db;

        public PropertyDealsController(FinPlannerDbContext db)
        {
            _db = db;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        #region Deals CRUD

        /// <summary>
        /// Get all property deals for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<PropertyDealDto>>> GetDeals([FromQuery] string? status = null)
        {
            var userId = GetUserId();
            var query = _db.PropertyDeals.Where(d => d.OwnerId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }

            var deals = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var dtos = deals.Select(d => new PropertyDealDto
            {
                Id = d.Id,
                Name = d.Name,
                Address = d.Address,
                BuildingType = d.BuildingType,
                Status = d.Status,
                AskingPrice = d.AskingPrice,
                EstimatedGrossRent = d.EstimatedGrossRent,
                CapRate = d.AskingPrice > 0 ? (d.EstimatedGrossRent / d.AskingPrice) * 100 : 0,
                CreatedAt = d.CreatedAt,
                DecisionDate = d.DecisionDate
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Get a single deal by ID with full details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDeal>> GetDeal(Guid id)
        {
            var userId = GetUserId();
            var deal = await _db.PropertyDeals
                .Include(d => d.SimulationResults!.OrderByDescending(s => s.RunDate).Take(5))
                .FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);

            if (deal == null)
                return NotFound();

            return Ok(deal);
        }

        /// <summary>
        /// Create a new property deal scenario
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PropertyDeal>> CreateDeal([FromBody] CreateDealRequest request)
        {
            var userId = GetUserId();

            var deal = new PropertyDeal
            {
                Id = Guid.NewGuid(),
                OwnerId = userId,
                Name = request.Name,
                Address = request.Address,
                BuildingType = request.BuildingType,
                Description = request.Description,
                Status = "Draft",
                AskingPrice = request.AskingPrice,
                EstimatedValue = request.EstimatedValue,
                StampDutyRate = request.StampDutyRate ?? 5.5m,
                LegalCosts = request.LegalCosts ?? 15000m,
                BuyersAgentFeeRate = request.BuyersAgentFeeRate ?? 0m,
                CapExReserve = request.CapExReserve,
                EstimatedGrossRent = request.EstimatedGrossRent,
                VacancyRatePercent = request.VacancyRatePercent ?? 5m,
                ManagementFeePercent = request.ManagementFeePercent ?? 7m,
                OutgoingsEstimate = request.OutgoingsEstimate,
                LoanAmount = request.LoanAmount,
                InterestRatePercent = request.InterestRatePercent ?? 6.5m,
                LoanTermYears = request.LoanTermYears ?? 25,
                RentVariancePercent = request.RentVariancePercent ?? 10m,
                VacancyVariancePercent = request.VacancyVariancePercent ?? 5m,
                InterestVariancePercent = request.InterestVariancePercent ?? 1m,
                CapitalGrowthPercent = request.CapitalGrowthPercent ?? 3m,
                CapitalGrowthVariancePercent = request.CapitalGrowthVariancePercent ?? 2m,
                RentalGrowthPercent = request.RentalGrowthPercent ?? 2.5m,
                VacancyGrowthPercent = request.VacancyGrowthPercent ?? 0m,
                ManagementGrowthPercent = request.ManagementGrowthPercent ?? 0m,
                OutgoingsGrowthPercent = request.OutgoingsGrowthPercent ?? 0m,
                DiscountRate = request.DiscountRate ?? 8m,
                HoldingPeriodYears = request.HoldingPeriodYears ?? 10,
                LeaseDetailsJson = request.LeaseDetailsJson,
                LoanDetailsJson = request.LoanDetailsJson,
                CreatedAt = DateTime.UtcNow
            };

            _db.PropertyDeals.Add(deal);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeal), new { id = deal.Id }, deal);
        }

        /// <summary>
        /// Update a property deal's inputs
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PropertyDeal>> UpdateDeal(Guid id, [FromBody] UpdateDealRequest request)
        {
            var userId = GetUserId();
            var deal = await _db.PropertyDeals.FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);

            if (deal == null)
                return NotFound();

            // Update only provided fields
            if (request.Name != null) deal.Name = request.Name;
            if (request.Address != null) deal.Address = request.Address;
            if (request.BuildingType != null) deal.BuildingType = request.BuildingType;
            if (request.Description != null) deal.Description = request.Description;
            if (request.AskingPrice.HasValue) deal.AskingPrice = request.AskingPrice.Value;
            if (request.EstimatedValue.HasValue) deal.EstimatedValue = request.EstimatedValue.Value;
            if (request.StampDutyRate.HasValue) deal.StampDutyRate = request.StampDutyRate.Value;
            if (request.LegalCosts.HasValue) deal.LegalCosts = request.LegalCosts.Value;
            if (request.BuyersAgentFeeRate.HasValue) deal.BuyersAgentFeeRate = request.BuyersAgentFeeRate.Value;
            if (request.CapExReserve.HasValue) deal.CapExReserve = request.CapExReserve.Value;
            if (request.EstimatedGrossRent.HasValue) deal.EstimatedGrossRent = request.EstimatedGrossRent.Value;
            if (request.VacancyRatePercent.HasValue) deal.VacancyRatePercent = request.VacancyRatePercent.Value;
            if (request.ManagementFeePercent.HasValue) deal.ManagementFeePercent = request.ManagementFeePercent.Value;
            if (request.OutgoingsEstimate.HasValue) deal.OutgoingsEstimate = request.OutgoingsEstimate.Value;
            if (request.LoanAmount.HasValue) deal.LoanAmount = request.LoanAmount.Value;
            if (request.InterestRatePercent.HasValue) deal.InterestRatePercent = request.InterestRatePercent.Value;
            if (request.LoanTermYears.HasValue) deal.LoanTermYears = request.LoanTermYears.Value;
            if (request.RentVariancePercent.HasValue) deal.RentVariancePercent = request.RentVariancePercent.Value;
            if (request.VacancyVariancePercent.HasValue) deal.VacancyVariancePercent = request.VacancyVariancePercent.Value;
            if (request.InterestVariancePercent.HasValue) deal.InterestVariancePercent = request.InterestVariancePercent.Value;
            if (request.CapitalGrowthPercent.HasValue) deal.CapitalGrowthPercent = request.CapitalGrowthPercent.Value;
            if (request.CapitalGrowthVariancePercent.HasValue) deal.CapitalGrowthVariancePercent = request.CapitalGrowthVariancePercent.Value;
            if (request.RentalGrowthPercent.HasValue) deal.RentalGrowthPercent = request.RentalGrowthPercent.Value;
            if (request.VacancyGrowthPercent.HasValue) deal.VacancyGrowthPercent = request.VacancyGrowthPercent.Value;
            if (request.ManagementGrowthPercent.HasValue) deal.ManagementGrowthPercent = request.ManagementGrowthPercent.Value;
            if (request.OutgoingsGrowthPercent.HasValue) deal.OutgoingsGrowthPercent = request.OutgoingsGrowthPercent.Value;
            if (request.DiscountRate.HasValue) deal.DiscountRate = request.DiscountRate.Value;
            if (request.HoldingPeriodYears.HasValue) deal.HoldingPeriodYears = request.HoldingPeriodYears.Value;
            if (request.SpreadsheetOverridesJson != null)
                deal.SpreadsheetOverridesJson = string.IsNullOrEmpty(request.SpreadsheetOverridesJson) ? null : request.SpreadsheetOverridesJson;
            if (request.LeaseDetailsJson != null)
                deal.LeaseDetailsJson = string.IsNullOrEmpty(request.LeaseDetailsJson) ? null : request.LeaseDetailsJson;
            if (request.LoanDetailsJson != null)
                deal.LoanDetailsJson = string.IsNullOrEmpty(request.LoanDetailsJson) ? null : request.LoanDetailsJson;
            if (request.Status != null) deal.Status = request.Status;

            // Validation: Discount rate must be >= Interest rate
            if (deal.DiscountRate < deal.InterestRatePercent)
            {
                return BadRequest($"Discount rate ({deal.DiscountRate}%) must be greater than or equal to the loan interest rate ({deal.InterestRatePercent}%). A lower discount rate implies less risk than a bank loan, which is unrealistic.");
            }

            deal.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(deal);
        }

        /// <summary>
        /// Record a decision on a deal (Buy/Pass/Uneconomic)
        /// </summary>
        [HttpPut("{id}/decision")]
        public async Task<IActionResult> RecordDecision(Guid id, [FromBody] RecordDecisionRequest request)
        {
            var userId = GetUserId();
            var deal = await _db.PropertyDeals.FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);

            if (deal == null)
                return NotFound();

            if (!new[] { "Buy", "Pass", "Uneconomic" }.Contains(request.Decision))
                return BadRequest("Decision must be 'Buy', 'Pass', or 'Uneconomic'");

            deal.Status = request.Decision;
            deal.DecisionRationale = request.Rationale;
            deal.DecisionDate = DateTime.UtcNow;
            deal.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Update deal status with optional comment/reason
        /// </summary>
        [HttpPost("{id}/status")]
        public async Task<IActionResult> RecordStatusChange(Guid id, [FromBody] UpdateStatusRequest request)
        {
            var userId = GetUserId();
            var deal = await _db.PropertyDeals.FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);

            if (deal == null)
                return NotFound();

            var oldStatus = deal.Status;

            // Update status
            deal.Status = request.Status;
            deal.UpdatedAt = DateTime.UtcNow;

            // Create history record
            var history = new DealStatusHistory
            {
                Id = Guid.NewGuid(),
                DealId = id,
                OldStatus = oldStatus,
                NewStatus = request.Status,
                Comment = request.Comment,
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow
            };

            _db.DealStatusHistory.Add(history);
            await _db.SaveChangesAsync();

            return Ok(deal);
        }

        /// <summary>
        /// Delete a deal (only drafts)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeal(Guid id)
        {
            var userId = GetUserId();
            var deal = await _db.PropertyDeals.FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);

            if (deal == null)
                return NotFound();

            if (deal.Status != "Draft")
                return BadRequest("Only draft deals can be deleted");

            _db.PropertyDeals.Remove(deal);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        #endregion

        #region Simulation Results

        /// <summary>
        /// Save simulation results for a deal
        /// </summary>
        [HttpPost("{id}/simulations")]
        public async Task<ActionResult<DealSimulationResult>> SaveSimulation(Guid id, [FromBody] SaveSimulationRequest request)
        {
            var userId = GetUserId();
            var deal = await _db.PropertyDeals.FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);

            if (deal == null)
                return NotFound();

            var result = new DealSimulationResult
            {
                Id = Guid.NewGuid(),
                DealId = id,
                RunDate = DateTime.UtcNow,
                Iterations = request.Iterations,
                MedianNPV = request.MedianNPV,
                P10NPV = request.P10NPV,
                P90NPV = request.P90NPV,
                MedianIRR = request.MedianIRR,
                P10IRR = request.P10IRR,
                P90IRR = request.P90IRR,
                CalculatedCapRate = request.CalculatedCapRate,
                RecommendedDecision = request.RecommendedDecision,
                NPVHistogramJson = request.NPVHistogramJson,
                IRRHistogramJson = request.IRRHistogramJson,
                YearlyDCFJson = request.YearlyDCFJson,
                InputsSnapshotJson = request.InputsSnapshotJson
            };

            // Update deal status to Analyzing if still Draft
            if (deal.Status == "Draft")
            {
                deal.Status = "Analyzing";
                deal.UpdatedAt = DateTime.UtcNow;
            }

            _db.DealSimulationResults.Add(result);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeal), new { id }, result);
        }

        /// <summary>
        /// Get simulation history for a deal
        /// </summary>
        [HttpGet("{id}/simulations")]
        public async Task<ActionResult<List<DealSimulationResult>>> GetSimulations(Guid id)
        {
            var userId = GetUserId();
            var deal = await _db.PropertyDeals.FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);

            if (deal == null)
                return NotFound();

            var simulations = await _db.DealSimulationResults
                .Where(s => s.DealId == id)
                .OrderByDescending(s => s.RunDate)
                .Take(10)
                .ToListAsync();

            return Ok(simulations);
        }

        #endregion
    }

    #region DTOs

    public class PropertyDealDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public string? BuildingType { get; set; }
        public string Status { get; set; } = "";
        public decimal AskingPrice { get; set; }
        public decimal EstimatedGrossRent { get; set; }
        public decimal CapRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DecisionDate { get; set; }
    }

    public class CreateDealRequest
    {
        public required string Name { get; set; }
        public string? Address { get; set; }
        public string? BuildingType { get; set; }
        public string? Description { get; set; }
        public decimal AskingPrice { get; set; }
        public decimal EstimatedValue { get; set; }
        public decimal? StampDutyRate { get; set; }
        public decimal? LegalCosts { get; set; }
        public decimal? BuyersAgentFeeRate { get; set; }
        public decimal CapExReserve { get; set; }
        public decimal EstimatedGrossRent { get; set; }
        public decimal? VacancyRatePercent { get; set; }
        public decimal? ManagementFeePercent { get; set; }
        public decimal OutgoingsEstimate { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal? InterestRatePercent { get; set; }
        public int? LoanTermYears { get; set; }
        public decimal? RentVariancePercent { get; set; }
        public decimal? VacancyVariancePercent { get; set; }
        public decimal? InterestVariancePercent { get; set; }
        public decimal? CapitalGrowthPercent { get; set; }
        public decimal? CapitalGrowthVariancePercent { get; set; }
        public decimal? RentalGrowthPercent { get; set; }
        public decimal? VacancyGrowthPercent { get; set; }
        public decimal? ManagementGrowthPercent { get; set; }
        public decimal? OutgoingsGrowthPercent { get; set; }
        public decimal? DiscountRate { get; set; }
        public int? HoldingPeriodYears { get; set; }
        public string? LeaseDetailsJson { get; set; }
        public string? LoanDetailsJson { get; set; }
    }

    public class UpdateDealRequest
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? BuildingType { get; set; }
        public string? Description { get; set; }
        public decimal? AskingPrice { get; set; }
        public decimal? EstimatedValue { get; set; }
        public decimal? StampDutyRate { get; set; }
        public decimal? LegalCosts { get; set; }
        public decimal? BuyersAgentFeeRate { get; set; }
        public decimal? CapExReserve { get; set; }
        public decimal? EstimatedGrossRent { get; set; }
        public decimal? VacancyRatePercent { get; set; }
        public decimal? ManagementFeePercent { get; set; }
        public decimal? OutgoingsEstimate { get; set; }
        public decimal? LoanAmount { get; set; }
        public decimal? InterestRatePercent { get; set; }
        public int? LoanTermYears { get; set; }
        public decimal? RentVariancePercent { get; set; }
        public decimal? VacancyVariancePercent { get; set; }
        public decimal? InterestVariancePercent { get; set; }
        public decimal? CapitalGrowthPercent { get; set; }
        public decimal? CapitalGrowthVariancePercent { get; set; }
        public decimal? RentalGrowthPercent { get; set; }
        public decimal? VacancyGrowthPercent { get; set; }
        public decimal? ManagementGrowthPercent { get; set; }
        public decimal? OutgoingsGrowthPercent { get; set; }
        public decimal? DiscountRate { get; set; }
        public int? HoldingPeriodYears { get; set; }
        public string? SpreadsheetOverridesJson { get; set; }
        public string? LeaseDetailsJson { get; set; }
        public string? LoanDetailsJson { get; set; }
        public string? Status { get; set; }
    }

    public class RecordDecisionRequest
    {
        public required string Decision { get; set; } // Buy, Pass, Uneconomic
        public string? Rationale { get; set; }
    }

    public class UpdateStatusRequest
    {
        public required string Status { get; set; }
        public string? Comment { get; set; }
    }

    public class SaveSimulationRequest
    {
        public int Iterations { get; set; }
        public decimal MedianNPV { get; set; }
        public decimal P10NPV { get; set; }
        public decimal P90NPV { get; set; }
        public decimal MedianIRR { get; set; }
        public decimal P10IRR { get; set; }
        public decimal P90IRR { get; set; }
        public decimal CalculatedCapRate { get; set; }
        public string RecommendedDecision { get; set; } = "";
        public string? NPVHistogramJson { get; set; }
        public string? IRRHistogramJson { get; set; }
        public string? YearlyDCFJson { get; set; }
        public string? InputsSnapshotJson { get; set; }
    }

    #endregion
}
