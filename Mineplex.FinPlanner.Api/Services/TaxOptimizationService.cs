using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface ITaxOptimizationService
    {
        Task InitializeParcelsAsync(Guid assetId);
        Task<decimal> CalculateTaxImpactAsync(Guid assetId, decimal unitsToSell, string method);
        Task<decimal> GetEstimatedTaxAsync(Guid portfolioId, int fiscalYear, decimal marginalTaxRate);
    }

    public class TaxOptimizationService : ITaxOptimizationService
    {
        private readonly FinPlannerDbContext _context;
        private readonly ILogger<TaxOptimizationService> _logger;

        public TaxOptimizationService(FinPlannerDbContext context, ILogger<TaxOptimizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeParcelsAsync(Guid assetId)
        {
            // 1. Clear existing parcels
            var existing = await _context.TaxParcels.Where(p => p.AssetId == assetId).ToListAsync();
            _context.TaxParcels.RemoveRange(existing);

            // 2. Fetch all Buy/Sell transactions chronologically
            var transactions = await _context.Transactions
                .Where(t => t.AssetId == assetId)
                .OrderBy(t => t.EffectiveDate)
                .ThenBy(t => t.AttachedOrder)
                .ToListAsync();

            var parcels = new List<TaxParcel>();

            foreach (var tx in transactions)
            {
                if (tx.Type == TransactionType.Buy || tx.Type == TransactionType.Deposit || tx.Type == TransactionType.Dividend) // Reinvested dividends? 
                {
                    // Create new parcel
                    parcels.Add(new TaxParcel
                    {
                        Id = Guid.NewGuid(),
                        AssetId = assetId,
                        AcquisitionDate = tx.EffectiveDate,
                        CostBase = tx.Amount > 0 && tx.Units > 0 ? tx.Amount / tx.Units : 0, // Fallback
                        Units = tx.Units,
                        RemainingUnits = tx.Units
                    });
                }
                else if (tx.Type == TransactionType.Sell || tx.Type == TransactionType.Withdrawal)
                {
                    // Deplete parcels (Default to FIFO for historical purposes unless we track specific ID history)
                    // NOTE: In a real system, we'd need to know WHICH parcel was sold. 
                    // For retro-fitting, FIFO is the standard assumption.
                    decimal unitsToDeplete = tx.Units;

                    foreach (var parcel in parcels.Where(p => p.RemainingUnits > 0).OrderBy(p => p.AcquisitionDate))
                    {
                        if (unitsToDeplete <= 0) break;

                        decimal quantity = Math.Min(unitsToDeplete, parcel.RemainingUnits);
                        parcel.RemainingUnits -= quantity;
                        unitsToDeplete -= quantity;
                    }
                }
            }

            // 3. Save only active parcels
            await _context.TaxParcels.AddRangeAsync(parcels.Where(p => p.RemainingUnits > 0));
            await _context.SaveChangesAsync();
        }

        public async Task<decimal> CalculateTaxImpactAsync(Guid assetId, decimal unitsToSell, string method)
        {
            var parcels = await _context.TaxParcels
                .Where(p => p.AssetId == assetId && p.RemainingUnits > 0)
                .Include(p => p.Asset).ThenInclude(a => a.CurrentPrice)
                .ToListAsync();

            if (!parcels.Any()) return 0;

            var currentPrice = parcels.First().Asset.CurrentPrice?.Price ?? 0;
            if (currentPrice == 0) return 0;

            List<TaxParcel> selectedParcels = new List<TaxParcel>();

            // Allocation Strategy
            switch (method?.ToUpper())
            {
                case "MAXGAIN":
                    // Sell lowest cost base first
                    selectedParcels = parcels.OrderBy(p => p.CostBase).ToList();
                    break;
                case "MINTAX":
                    // High Cost Base first, or Long Term Hold (> 12 months)
                    selectedParcels = parcels
                        .OrderByDescending(p => CalculateEstimatedTax(p, currentPrice)) // Sell those with lowest tax impact (e.g. losses or discounted gains)
                                                                                        // Actually, simpler heuristic: Sell Highest Cost Base FIRST (minimizes gain), 
                                                                                        // THEN check for CGT discount.
                                                                                        // Let's refine: We want to MINIMIZE (Gain * TaxRate).
                                                                                        // If Held > 12mo, TaxRate is 50%. Else 100%.
                        .OrderBy(p => (currentPrice - p.CostBase) * (p.AcquisitionDate < DateTime.UtcNow.AddYears(-1) ? 0.5m : 1.0m))
                        .ToList();
                    break;
                case "FIFO":
                default:
                    selectedParcels = parcels.OrderBy(p => p.AcquisitionDate).ToList();
                    break;
            }

            decimal totalGain = 0;
            decimal remainingToSell = unitsToSell;

            foreach (var parcel in selectedParcels)
            {
                if (remainingToSell <= 0) break;

                decimal units = Math.Min(remainingToSell, parcel.RemainingUnits);

                decimal cost = units * parcel.CostBase;
                decimal proceeds = units * currentPrice;
                decimal grossGain = proceeds - cost;

                // CGT Discount
                bool isLongTerm = parcel.AcquisitionDate < DateTime.UtcNow.AddYears(-1);
                decimal taxableGain = grossGain > 0 && isLongTerm ? grossGain * 0.5m : grossGain;

                totalGain += taxableGain;
                remainingToSell -= units;
            }

            return totalGain; // This is the "Taxable Income" added, not the raw tax paid (need marginal rate for that)
        }

        private decimal CalculateEstimatedTax(TaxParcel p, decimal currentPrice)
        {
            decimal gain = (currentPrice - p.CostBase);
            bool isLongTerm = p.AcquisitionDate < DateTime.UtcNow.AddYears(-1);
            if (gain > 0 && isLongTerm) gain *= 0.5m;
            return gain;
        }

        public async Task<decimal> GetEstimatedTaxAsync(Guid portfolioId, int fiscalYear, decimal marginalTaxRate)
        {
            // Placeholder for summary logic
            // In a real app, this would sum up all Realized Gains for the FY
            return 0;
        }
    }
}
