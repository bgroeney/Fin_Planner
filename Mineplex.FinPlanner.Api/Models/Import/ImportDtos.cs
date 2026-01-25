namespace Mineplex.FinPlanner.Api.Models.Import
{
    public class ImportPreviewDto
    {
        public required string FileType { get; set; } // PortfolioValuation or TransactionListing
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public List<ImportRecordDto> PreviewRecords { get; set; } = new();
    }

    public class ImportRecordDto
    {
        public string Asset { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public decimal Units { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string AssetClass { get; set; } = string.Empty; // From CSV
        public bool IsDuplicate { get; set; }
    }

    public class ConfirmImportDto
    {
        public Guid PortfolioId { get; set; }
        public required string FileContentBase64 { get; set; }
        public required string FileName { get; set; }
        public bool IncludeDuplicates { get; set; }
    }
}
