using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using System.Security.Claims;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommercialPropertyController : ControllerBase
    {
        private readonly FinPlannerDbContext _db;
        private readonly ICommercialPropertyService _service;

        public CommercialPropertyController(FinPlannerDbContext db, ICommercialPropertyService service)
        {
            _db = db;
            _service = service;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        #region Properties CRUD

        /// <summary>
        /// Get all commercial properties for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<CommercialProperty>>> GetProperties()
        {
            var userId = GetUserId();
            var properties = await _db.CommercialProperties
                .Where(p => p.OwnerId == userId)
                .Include(p => p.Valuations.OrderByDescending(v => v.Date).Take(1))
                .Include(p => p.Leases.Where(l => l.IsActive))
                .OrderBy(p => p.Address)
                .ToListAsync();

            return Ok(properties);
        }

        /// <summary>
        /// Get a single property by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CommercialProperty>> GetProperty(Guid id)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .Include(p => p.Valuations.OrderByDescending(v => v.Date))
                .Include(p => p.Leases)
                .Include(p => p.LedgerEntries.OrderByDescending(l => l.Date).Take(50))
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            return Ok(property);
        }

        /// <summary>
        /// Create a new commercial property
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CommercialProperty>> CreateProperty([FromBody] CreatePropertyRequest request)
        {
            var userId = GetUserId();

            var property = new CommercialProperty
            {
                Id = Guid.NewGuid(),
                OwnerId = userId,
                Address = request.Address,
                TitleReference = request.TitleReference,
                TotalGFA = request.TotalGFA,
                PurchasePrice = request.PurchasePrice,
                PurchaseDate = request.PurchaseDate,
                BuildingType = request.BuildingType,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _db.CommercialProperties.Add(property);

            // Add initial valuation at purchase price
            var initialValuation = new PropertyValuation
            {
                Id = Guid.NewGuid(),
                PropertyId = property.Id,
                Date = request.PurchaseDate,
                Value = request.PurchasePrice,
                Source = "Internal",
                Notes = "Purchase price",
                CreatedAt = DateTime.UtcNow
            };
            _db.PropertyValuations.Add(initialValuation);

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, property);
        }

        /// <summary>
        /// Update a commercial property
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CommercialProperty>> UpdateProperty(Guid id, [FromBody] UpdatePropertyRequest request)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            property.Address = request.Address ?? property.Address;
            property.TitleReference = request.TitleReference ?? property.TitleReference;
            property.TotalGFA = request.TotalGFA ?? property.TotalGFA;
            property.BuildingType = request.BuildingType ?? property.BuildingType;
            property.Description = request.Description ?? property.Description;

            await _db.SaveChangesAsync();

            return Ok(property);
        }

        /// <summary>
        /// Delete a commercial property
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(Guid id)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            _db.CommercialProperties.Remove(property);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Valuations

        /// <summary>
        /// Get valuation history for a property
        /// </summary>
        [HttpGet("{id}/valuations")]
        public async Task<ActionResult<List<PropertyValuation>>> GetValuations(Guid id)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            var valuations = await _db.PropertyValuations
                .Where(v => v.PropertyId == id)
                .OrderByDescending(v => v.Date)
                .ToListAsync();

            return Ok(valuations);
        }

        /// <summary>
        /// Add a valuation to a property
        /// </summary>
        [HttpPost("{id}/valuations")]
        public async Task<ActionResult<PropertyValuation>> AddValuation(Guid id, [FromBody] CreateValuationRequest request)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            var valuation = new PropertyValuation
            {
                Id = Guid.NewGuid(),
                PropertyId = id,
                Date = request.Date,
                Value = request.Value,
                Source = request.Source,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _db.PropertyValuations.Add(valuation);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetValuations), new { id }, valuation);
        }

        #endregion

        #region Ledger

        /// <summary>
        /// Get ledger entries for a property
        /// </summary>
        [HttpGet("{id}/ledger")]
        public async Task<ActionResult<List<PropertyLedger>>> GetLedger(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            var query = _db.PropertyLedgerEntries.Where(l => l.PropertyId == id);

            if (from.HasValue)
                query = query.Where(l => l.Date >= from.Value);
            if (to.HasValue)
                query = query.Where(l => l.Date <= to.Value);

            var entries = await query
                .OrderByDescending(l => l.Date)
                .ToListAsync();

            return Ok(entries);
        }

        /// <summary>
        /// Add a ledger entry
        /// </summary>
        [HttpPost("{id}/ledger")]
        public async Task<ActionResult<PropertyLedger>> AddLedgerEntry(Guid id, [FromBody] CreateLedgerEntryRequest request)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            var entry = new PropertyLedger
            {
                Id = Guid.NewGuid(),
                PropertyId = id,
                Date = request.Date,
                Type = request.Type,
                Amount = request.Amount,
                IsIncome = request.IsIncome,
                Description = request.Description,
                FileUploadId = request.FileUploadId,
                CreatedAt = DateTime.UtcNow
            };

            _db.PropertyLedgerEntries.Add(entry);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLedger), new { id }, entry);
        }

        #endregion

        #region Leases

        /// <summary>
        /// Get lease profiles for a property
        /// </summary>
        [HttpGet("{id}/leases")]
        public async Task<ActionResult<List<LeaseProfile>>> GetLeases(Guid id)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            var leases = await _db.LeaseProfiles
                .Where(l => l.PropertyId == id)
                .OrderByDescending(l => l.IsActive)
                .ThenBy(l => l.LeaseEnd)
                .ToListAsync();

            return Ok(leases);
        }

        /// <summary>
        /// Add or update a lease profile
        /// </summary>
        [HttpPost("{id}/leases")]
        public async Task<ActionResult<LeaseProfile>> AddLease(Guid id, [FromBody] CreateLeaseRequest request)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            var lease = new LeaseProfile
            {
                Id = Guid.NewGuid(),
                PropertyId = id,
                TenantName = request.TenantName,
                UnitReference = request.UnitReference,
                LeaseStart = request.LeaseStart,
                LeaseEnd = request.LeaseEnd,
                OptionPeriod = request.OptionPeriod,
                CurrentRent = request.CurrentRent,
                ReviewType = request.ReviewType,
                ReviewPercentage = request.ReviewPercentage,
                OutgoingsRecoverable = request.OutgoingsRecoverable,
                GLA = request.GLA,
                IsActive = true,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _db.LeaseProfiles.Add(lease);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeases), new { id }, lease);
        }

        /// <summary>
        /// Update a lease profile
        /// </summary>
        [HttpPut("{propertyId}/leases/{leaseId}")]
        public async Task<ActionResult<LeaseProfile>> UpdateLease(Guid propertyId, Guid leaseId, [FromBody] UpdateLeaseRequest request)
        {
            var userId = GetUserId();
            var property = await _db.CommercialProperties
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.OwnerId == userId);

            if (property == null)
                return NotFound();

            var lease = await _db.LeaseProfiles
                .FirstOrDefaultAsync(l => l.Id == leaseId && l.PropertyId == propertyId);

            if (lease == null)
                return NotFound();

            lease.TenantName = request.TenantName ?? lease.TenantName;
            lease.UnitReference = request.UnitReference ?? lease.UnitReference;
            lease.LeaseStart = request.LeaseStart ?? lease.LeaseStart;
            lease.LeaseEnd = request.LeaseEnd ?? lease.LeaseEnd;
            lease.OptionPeriod = request.OptionPeriod ?? lease.OptionPeriod;
            lease.CurrentRent = request.CurrentRent ?? lease.CurrentRent;
            lease.ReviewType = request.ReviewType ?? lease.ReviewType;
            lease.ReviewPercentage = request.ReviewPercentage ?? lease.ReviewPercentage;
            lease.OutgoingsRecoverable = request.OutgoingsRecoverable ?? lease.OutgoingsRecoverable;
            lease.GLA = request.GLA ?? lease.GLA;
            lease.IsActive = request.IsActive ?? lease.IsActive;
            lease.Notes = request.Notes ?? lease.Notes;

            await _db.SaveChangesAsync();

            return Ok(lease);
        }

        #endregion

        #region Dashboard

        /// <summary>
        /// Get dashboard summary with yield, vacancy, and cashflow data
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<PropertyDashboardSummary>> GetDashboard()
        {
            var userId = GetUserId();
            return Ok(await _service.GetDashboardSummaryAsync(userId));
        }

        #endregion
    }
}
