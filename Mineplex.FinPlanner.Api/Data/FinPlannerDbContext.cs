using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Data
{
    public class FinPlannerDbContext : DbContext
    {
        public FinPlannerDbContext(DbContextOptions<FinPlannerDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetCategory> AssetCategories { get; set; }
        public DbSet<Holding> Holdings { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Decision> Decisions { get; set; }
        public DbSet<AIRecommendation> AIRecommendations { get; set; }
        public DbSet<PerformanceSnapshot> PerformanceSnapshots { get; set; }
        public DbSet<TaxParcel> TaxParcels { get; set; }

        // Price System
        public DbSet<CurrentPrice> CurrentPrices { get; set; }
        public DbSet<HistoricalPrice> HistoricalPrices { get; set; }
        public DbSet<PriceSource> PriceSources { get; set; }
        public DbSet<AssetPriceSourceOverride> AssetPriceSourceOverrides { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        // File History & Imported Prices
        public DbSet<FileUpload> FileUploads { get; set; }
        public DbSet<ImportedAssetPrice> ImportedAssetPrices { get; set; }

        public DbSet<DecisionLog> DecisionLogs { get; set; }

        // Commercial Property Module
        public DbSet<CommercialProperty> CommercialProperties { get; set; }
        public DbSet<PropertyValuation> PropertyValuations { get; set; }
        public DbSet<PropertyLedger> PropertyLedgerEntries { get; set; }
        public DbSet<LeaseProfile> LeaseProfiles { get; set; }

        // Property Deals (Acquisition Planner)
        public DbSet<PropertyDeal> PropertyDeals { get; set; }
        public DbSet<DealSimulationResult> DealSimulationResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Decision Logs
            modelBuilder.Entity<DecisionLog>()
                .HasIndex(dl => dl.DecisionId); // Optimize history lookup

            // Seed default data if needed or configure relationships
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Holding>()
                .Property(h => h.Units)
                .HasPrecision(18, 6);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Units)
                .HasPrecision(18, 6);

            // Portfolio Cascade Delete Configuration
            // When a Portfolio is deleted, cascade to related entities
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Portfolio)
                .WithMany()
                .HasForeignKey(a => a.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AssetCategory>()
                .HasOne(c => c.Portfolio)
                .WithMany()
                .HasForeignKey(c => c.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Decision>()
                .HasOne(d => d.Portfolio)
                .WithMany()
                .HasForeignKey(d => d.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PerformanceSnapshot>()
                .HasIndex(ps => ps.PortfolioId);
            modelBuilder.Entity<PerformanceSnapshot>()
                .HasOne<Portfolio>()
                .WithMany()
                .HasForeignKey(ps => ps.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Account Cascade Delete Configuration
            // When an Account is deleted, cascade to holdings and transactions
            modelBuilder.Entity<Holding>()
                .HasOne(h => h.Account)
                .WithMany()
                .HasForeignKey(h => h.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Price System Configuration

            // CurrentPrice: One price per asset (unique)
            modelBuilder.Entity<CurrentPrice>()
                .HasIndex(cp => cp.AssetId)
                .IsUnique();

            modelBuilder.Entity<CurrentPrice>()
                .HasIndex(cp => cp.LastUpdated);

            // HistoricalPrice: Unique constraint on AssetId + Date
            modelBuilder.Entity<HistoricalPrice>()
                .HasIndex(hp => new { hp.AssetId, hp.Date })
                .IsUnique();

            // File Uploads
            modelBuilder.Entity<FileUpload>()
                .HasIndex(f => f.PortfolioId);

            modelBuilder.Entity<FileUpload>()
                .HasOne(f => f.Portfolio)
                .WithMany()
                .HasForeignKey(f => f.PortfolioId)
                .OnDelete(DeleteBehavior.SetNull); // Preserve file history when portfolio is deleted

            modelBuilder.Entity<FileUpload>()
                .HasOne(f => f.UploadedBy)
                .WithMany()
                .HasForeignKey(f => f.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull); // Keep history even if user is deleted

            // Imported Asset Prices
            modelBuilder.Entity<ImportedAssetPrice>()
                .HasIndex(p => new { p.AssetId, p.ValuationDate });

            modelBuilder.Entity<ImportedAssetPrice>()
                .HasOne(p => p.FileUpload)
                .WithMany()
                .HasForeignKey(p => p.FileUploadId)
                .OnDelete(DeleteBehavior.Cascade); // Delete prices if file is deleted

            // Document association to Deals
            modelBuilder.Entity<FileUpload>()
                .HasIndex(f => f.PropertyDealId);

            modelBuilder.Entity<FileUpload>()
                .HasOne(f => f.PropertyDeal)
                .WithMany()
                .HasForeignKey(f => f.PropertyDealId)
                .OnDelete(DeleteBehavior.Cascade); // Files of a deal are deleted with the deal

            // PriceSource: Index on priority for efficient ordering
            modelBuilder.Entity<PriceSource>()
                .HasIndex(ps => ps.Priority);

            modelBuilder.Entity<PriceSource>()
                .HasIndex(ps => ps.Code)
                .IsUnique();

            // SystemSetting: Key is the primary key
            modelBuilder.Entity<SystemSetting>()
                .HasKey(ss => ss.Key);

            // Seed default price sources
            modelBuilder.Entity<PriceSource>().HasData(
                new PriceSource
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Yahoo Finance",
                    Code = "YAHOO",
                    Priority = 1,
                    IsEnabled = true,
                    RateLimitPerMinute = 2000
                },
                new PriceSource
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Morningstar Australia",
                    Code = "MORNINGSTAR_AU",
                    Priority = 2,
                    IsEnabled = true,
                    RateLimitPerMinute = 120 // Conservative rate limit for web scraping
                },
                new PriceSource
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Alpha Vantage",
                    Code = "ALPHAVANTAGE",
                    Priority = 3,
                    IsEnabled = false,
                    RateLimitPerMinute = 5
                },
                new PriceSource
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Polygon.io",
                    Code = "POLYGON",
                    Priority = 4,
                    IsEnabled = false,
                    RateLimitPerMinute = 5
                },
                new PriceSource
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Imported File",
                    Code = "IMPORTED",
                    Priority = 10,
                    IsEnabled = true,
                    RateLimitPerMinute = 10000 // Internal, so high limit
                }
            );

            // Seed default system settings
            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting
                {
                    Key = "PriceUpdateIntervalMinutes",
                    Value = "15",
                    Description = "How often to update market prices (in minutes)",
                    LastModified = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Commercial Property Module Configuration
            modelBuilder.Entity<CommercialProperty>()
                .HasIndex(cp => cp.OwnerId);

            modelBuilder.Entity<PropertyValuation>()
                .HasIndex(pv => new { pv.PropertyId, pv.Date });

            modelBuilder.Entity<PropertyLedger>()
                .HasIndex(pl => new { pl.PropertyId, pl.Date });

            modelBuilder.Entity<LeaseProfile>()
                .HasIndex(lp => lp.PropertyId);

            modelBuilder.Entity<LeaseProfile>()
                .HasIndex(lp => lp.LeaseEnd); // For vacancy queries
        }
    }
}
