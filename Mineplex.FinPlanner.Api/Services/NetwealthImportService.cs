using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Models.Import;
using Mineplex.FinPlanner.Api.Services.Import;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface INetwealthImportService
    {
        Task<ImportPreviewDto> PreviewImportAsync(ConfirmImportDto dto);
        Task ConfirmImportAsync(Guid userId, ConfirmImportDto dto);

        // File Management
        Task<List<FileUploadDto>> GetUploadHistoryAsync(Guid portfolioId);
        Task<string?> GetFileDownloadUrlAsync(Guid fileId, Guid userId); // Return storage path or unsigned URL
        Task DeleteImportAsync(Guid fileId, Guid userId);
        Task ToggleImportActiveAsync(Guid fileId, Guid userId, bool isActive);
    }

    public class NetwealthImportService : INetwealthImportService
    {
        private readonly FinPlannerDbContext _context;
        private readonly ITransactionService _transactionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly INetwealthCsvParser _parser;

        public NetwealthImportService(FinPlannerDbContext context, ITransactionService transactionService, IFileStorageService fileStorageService, INetwealthCsvParser parser)
        {
            _context = context;
            _transactionService = transactionService;
            _fileStorageService = fileStorageService;
            _parser = parser;
        }

        public async Task<ImportPreviewDto> PreviewImportAsync(ConfirmImportDto dto)
        {
            var content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(dto.FileContentBase64));

            // Use the injected parser
            var preview = _parser.Parse(content);

            // Duplicate Detection
            if (dto.PortfolioId != Guid.Empty && preview.PreviewRecords.Any())
            {
                var accountIds = await _context.Accounts
                    .Where(a => a.PortfolioId == dto.PortfolioId)
                    .Select(a => a.Id)
                    .ToListAsync();

                if (accountIds.Any())
                {
                    // Fetch existing transactions to compare
                    // Only fetch relevant fields to check for duplicates
                    var existingTxns = await _context.Transactions
                        .Where(t => accountIds.Contains(t.AccountId))
                        .Select(t => new { t.EffectiveDate, t.Amount, t.Units, AssetCode = t.Asset.Symbol }) // Simplification
                        .ToListAsync();

                    foreach (var record in preview.PreviewRecords)
                    {
                        // Loose matching logic for now as we don't have perfect AssetId match yet
                        var paymentDate = record.Date ?? DateTime.MinValue;
                        var isDuplicate = existingTxns.Any(t =>
                            t.EffectiveDate.Date == paymentDate.Date &&
                            Math.Abs(t.Amount - record.Amount) < 0.01m &&
                            Math.Abs(t.Units - record.Units) < 0.001m &&
                            (t.AssetCode == record.Code || string.IsNullOrEmpty(record.Code)) // Check code if available
                        );

                        record.IsDuplicate = isDuplicate;
                    }
                }
            }

            return preview;
        }

        public async Task ConfirmImportAsync(Guid userId, ConfirmImportDto dto)
        {
            var preview = await PreviewImportAsync(dto); // Re-parse to be safe

            // Find or Create Account
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.PortfolioId == dto.PortfolioId && a.AccountNumber == preview.AccountNumber);

            if (account == null)
            {
                account = new Account
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = dto.PortfolioId,
                    AccountNumber = preview.AccountNumber,
                    AccountName = preview.AccountName,
                    Provider = "Netwealth",
                    ProductType = "Wrap"
                };
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
            }

            // 1. Save File and Create FileUpload Record
            var fileBytes = Convert.FromBase64String(dto.FileContentBase64);
            var storagePath = await _fileStorageService.SaveFileAsync(dto.PortfolioId, dto.FileName, fileBytes);

            // Try to approximate valuation date. 
            // For Transaction Listing, it's roughly the last transaction date.
            // For Portfolio Valuation, we should have parsed it or default to Today.
            DateTime? valuationDate = null;
            if (preview.FileType == "PortfolioValuation")
            {
                // Simplified: assume Today for now if not parsed, or use one of the record dates if they existed
                valuationDate = DateTime.UtcNow.Date;
            }
            else if (preview.PreviewRecords.Any())
            {
                valuationDate = preview.PreviewRecords.Max(r => r.Date);
            }

            var fileUpload = new FileUpload
            {
                Id = Guid.NewGuid(),
                PortfolioId = dto.PortfolioId,
                UploadedByUserId = userId,
                FileName = dto.FileName,
                FileType = preview.FileType,
                StoragePath = storagePath,
                FileSizeBytes = fileBytes.Length,
                UploadedAt = DateTime.UtcNow,
                ValuationDate = valuationDate,
                RecordsProcessed = preview.PreviewRecords.Count,
                AccountId = account.Id,
                IsActive = true
            };

            _context.FileUploads.Add(fileUpload);
            // Save here to get ID? No need, Guid is generated.
            // But we might want to save to ensure FK validity if we add related records in loop? 
            // EF handles it if we add them to context.

            // Sync Assets & Categories logic...
            // For MVP, if asset doesn't exist, create it.
            // In a real app we'd map codes to a master list.

            // Performance Optimization: Pre-fetch assets, holdings and transactions to avoid N+1 queries in the loop
            var symbols = preview.PreviewRecords
                .Select(r => r.Code)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var assetsCache = await _context.Assets
                .Where(a => symbols.Contains(a.Symbol))
                .ToDictionaryAsync(a => a.Symbol, a => a);

            var holdingsCache = new Dictionary<Guid, Holding>();
            if (preview.FileType == "PortfolioValuation")
            {
                holdingsCache = await _context.Holdings
                    .Where(h => h.AccountId == account.Id)
                    .ToDictionaryAsync(h => h.AssetId, h => h);
            }

            var transactionCache = new HashSet<(Guid AssetId, DateTime? Date, decimal Amount)>();
            if (preview.FileType == "TransactionListing")
            {
                var existingTxns = await _context.Transactions
                    .Where(t => t.AccountId == account.Id)
                    .Select(t => new { t.AssetId, t.EffectiveDate, t.Amount })
                    .ToListAsync();

                foreach (var t in existingTxns)
                {
                    transactionCache.Add((t.AssetId, t.EffectiveDate, t.Amount));
                }
            }

            foreach (var rec in preview.PreviewRecords)
            {
                if (string.IsNullOrEmpty(rec.Code)) continue; // Skip cash/unknown for now unless it has a special code

                if (!assetsCache.TryGetValue(rec.Code, out var asset))
                {
                    asset = new Asset
                    {
                        Id = Guid.NewGuid(),
                        Symbol = rec.Code,
                        Name = rec.Asset,
                        AssetType = "Unknown" // Can refine based on "AssetClass" if available or user mapping
                    };
                    _context.Assets.Add(asset);
                    await _context.SaveChangesAsync();
                    assetsCache[rec.Code] = asset;
                }

                if (preview.FileType == "PortfolioValuation")
                {
                    // Upsert Holding
                    if (!holdingsCache.TryGetValue(asset.Id, out var holding))
                    {
                        holding = new Holding
                        {
                            Id = Guid.NewGuid(),
                            AccountId = account.Id,
                            AssetId = asset.Id,
                            Units = rec.Units,
                            AvgCost = 0, // Valuation file usually has "Avg cost", can parse from 'Amount'/Units if provided or specific column
                            CurrentValue = rec.Amount
                        };
                        _context.Holdings.Add(holding);
                        holdingsCache[asset.Id] = holding;
                    }

                    holding.Units = rec.Units;
                    holding.CurrentValue = rec.Amount;

                    // Create ImportedAssetPrice for history/fallback
                    if (rec.Units != 0 && valuationDate.HasValue)
                    {
                        var unitPrice = rec.Amount / rec.Units;
                        if (unitPrice > 0) // Avoid negative/zero prices for weird data
                        {
                            var importedPrice = new ImportedAssetPrice
                            {
                                Id = Guid.NewGuid(),
                                FileUploadId = fileUpload.Id,
                                AssetId = asset.Id,
                                UnitPrice = Math.Abs(unitPrice), // Prices usually positive
                                Units = rec.Units,
                                TotalValue = rec.Amount,
                                ValuationDate = valuationDate.Value
                            };
                            _context.ImportedAssetPrices.Add(importedPrice);
                        }
                    }
                }
                else if (preview.FileType == "TransactionListing")
                {
                    // Insert Transaction if not exists (check by date/amount/asset)
                    var exists = transactionCache.Contains((asset.Id, rec.Date, rec.Amount));

                    if (!exists && (!rec.IsDuplicate || dto.IncludeDuplicates)) // Allow override
                    {
                        // Check if units is negative or positive based on type?
                        // Netwealth CSV units are signed: - for sale, + for buy.

                        _context.Transactions.Add(new Transaction
                        {
                            Id = Guid.NewGuid(),
                            AccountId = account.Id,
                            AssetId = asset.Id,
                            EffectiveDate = rec.Date ?? DateTime.UtcNow,
                            Type = DetermineTransactionType(rec.Type, ""),
                            Units = rec.Units,
                            Amount = rec.Amount,
                            Narration = rec.Asset,
                            ExternalReferenceId = GenerateTransactionHash(rec.Date, rec.Code, rec.Type, rec.Units, rec.Amount),
                            FileUploadId = fileUpload.Id
                        });

                        // Add to cache to avoid duplicates within the same import if they appear twice in the file
                        transactionCache.Add((asset.Id, rec.Date, rec.Amount));
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Generate Retrospective Decisions for Buy/Sell transactions
            if (preview.FileType == "TransactionListing")
            {
                var processedTransactions = await _context.Transactions
                    .Where(t => t.AccountId == account.Id)
                    .Include(t => t.Asset) // Needed for decision title
                    .ToListAsync();

                // We only want to generate decisions for the records we just processed/added.
                // However, since we don't have the exact IDs easily, we can check for missing decisions for ALL transactions 
                // in this account matching the imported criteria. This also fixes missing decisions for previously imported data.

                var existingDecisions = await _context.Decisions
                    .Where(d => d.PortfolioId == dto.PortfolioId)
                    .ToListAsync();

                var newDecisions = new List<Decision>();

                foreach (var txn in processedTransactions)
                {
                    if (txn.Type != TransactionType.Buy && txn.Type != TransactionType.Sell) continue;

                    // Check if decision exists for this transaction
                    // We match mostly on timestamp and asset to find "the decision that caused this trade"
                    // Ideally we'd link them, but for retroactive, fuzzy match is okay.
                    // Or we check if there's a decision with ImplementedAt == EffectiveDate and similar details.

                    var alreadyExists = existingDecisions.Any(d =>
                        d.ImplementedAt.HasValue &&
                        d.ImplementedAt.Value.Date == txn.EffectiveDate.Date &&
                        d.Title != null && d.Title.Contains(txn.Asset!.Symbol) &&
                        d.Type == "Manual" // Imported/Retroactive are Manual
                    );

                    if (!alreadyExists)
                    {
                        var action = txn.Type.ToString();
                        var decision = new Decision
                        {
                            Id = Guid.NewGuid(),
                            PortfolioId = dto.PortfolioId,
                            Title = $"{action} {txn.Units:F0} {txn.Asset.Symbol}",
                            Type = "Manual",
                            Status = "Implemented",
                            Rationale = $"Retrospective decision generated from imported transaction: {action} {txn.Units:F4} units of {txn.Asset.Name} on {txn.EffectiveDate:d}.",
                            SnapshotBefore = "{}", // No snapshot for historic
                            SnapshotAfter = "{}",
                            CreatedAt = txn.EffectiveDate, // Create date = trade date for sorting
                            ApprovedAt = txn.EffectiveDate,
                            ImplementedAt = txn.EffectiveDate,
                            FileUploadId = fileUpload.Id
                        };
                        newDecisions.Add(decision);
                        existingDecisions.Add(decision); // Add to local list to prevent duplicates if multiple txns match same day/asset (though unlikely exact same)
                    }
                }

                if (newDecisions.Any())
                {
                    _context.Decisions.AddRange(newDecisions);
                    await _context.SaveChangesAsync();
                }
            }

            // Trigger holding recalculation (updates units and avg cost based on transactions)
            await _transactionService.RecalculateHoldingsAsync(account.Id);
        }

        private TransactionType DetermineTransactionType(string description, string subType)
        {
            var desc = description?.ToLowerInvariant() ?? "";

            if (desc.Contains("buy") || desc.Contains("purchase") || desc.Contains("application")) return TransactionType.Buy;
            if (desc.Contains("sell") || desc.Contains("sale") || desc.Contains("redemption")) return TransactionType.Sell;
            if (desc.Contains("distribution") || desc.Contains("dividend") || desc.Contains("income")) return TransactionType.Distribution;
            if (desc.Contains("fee") || desc.Contains("admin") || desc.Contains("adviser")) return TransactionType.Fee_Indirect;
            if (desc.Contains("deposit") || desc.Contains("contribution")) return TransactionType.Deposit;
            if (desc.Contains("withdrawal") || desc.Contains("pension")) return TransactionType.Withdrawal;

            return TransactionType.Fee_Direct; // Default fallback to generic fee or other
        }

        private string GenerateTransactionHash(DateTime? date, string code, string type, decimal units, decimal amount)
        {
            var d = date?.ToString("yyyyMMdd") ?? "NODATE";
            var raw = $"{d}|{code}|{type}|{units:F4}|{amount:F2}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task<List<FileUploadDto>> GetUploadHistoryAsync(Guid portfolioId)
        {
            return await _context.FileUploads
                .Where(f => f.PortfolioId == portfolioId)
                .OrderByDescending(f => f.UploadedAt)
                .Select(f => new FileUploadDto
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    FileType = f.FileType,
                    FileSizeBytes = f.FileSizeBytes,
                    UploadedAt = f.UploadedAt,
                    ValuationDate = f.ValuationDate,
                    IsActive = f.IsActive,
                    RecordsProcessed = f.RecordsProcessed, // You might need to add this property to FileUpload entity or models first? 
                                                           // Wait, checking model... Yes, RecordsProcessed was added in FileUploadModels.cs
                    AccountName = f.Account != null ? f.Account.AccountName : "Unknown"
                })
                .ToListAsync();
        }

        public async Task<string?> GetFileDownloadUrlAsync(Guid fileId, Guid userId)
        {
            var file = await _context.FileUploads.FindAsync(fileId);
            if (file == null) return null;

            // Security check: userId owners... for now skipping strict check or assuming controller checked portfolio access
            // Ideally we check _context.Portfolios.FirstOrDefault(p => p.Id == file.PortfolioId && p.OwnerId == userId)

            return file.StoragePath; // Controller will use this to serve file
        }

        public async Task DeleteImportAsync(Guid fileId, Guid userId)
        {
            var file = await _context.FileUploads.FindAsync(fileId);
            if (file == null) return;

            var accountId = file.AccountId;

            // 1. Delete transactions and decisions
            var transactions = _context.Transactions.Where(t => t.FileUploadId == fileId);
            _context.Transactions.RemoveRange(transactions);

            var decisions = _context.Decisions.Where(d => d.FileUploadId == fileId);
            _context.Decisions.RemoveRange(decisions);

            // 2. Delete actual file
            await _fileStorageService.DeleteFileAsync(file.StoragePath);

            // 3. Remove record
            _context.FileUploads.Remove(file);
            await _context.SaveChangesAsync();

            // 4. Trigger recalculation
            if (accountId.HasValue)
            {
                await _transactionService.RecalculateHoldingsAsync(accountId.Value);
            }
        }

        public async Task ToggleImportActiveAsync(Guid fileId, Guid userId, bool isActive)
        {
            var file = await _context.FileUploads
                .Include(f => f.Account)
                .FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null) return;

            file.IsActive = isActive;
            await _context.SaveChangesAsync();

            // 1. Handle associated Decisions
            var decisions = await _context.Decisions
                .Where(d => d.FileUploadId == fileId)
                .ToListAsync();

            foreach (var d in decisions)
            {
                if (!isActive)
                {
                    // If unloading, revert "Implemented" to "Draft"
                    if (d.Status == "Implemented")
                    {
                        d.Status = "Draft";
                        d.ImplementedAt = null;
                    }
                }
                else
                {
                    // If reloading, restore "Draft" to "Implemented" if they were auto-generated
                    if (d.Status == "Draft")
                    {
                        d.Status = "Implemented";
                        d.ImplementedAt = d.ApprovedAt ?? d.CreatedAt;
                    }
                }
            }

            await _context.SaveChangesAsync();

            // 2. Trigger recalculation for the account
            if (file.AccountId.HasValue)
            {
                await _transactionService.RecalculateHoldingsAsync(file.AccountId.Value);
            }
        }
    }
}
