using System;

namespace Mineplex.FinPlanner.Api.Models
{
    public class CreateLedgerEntryRequest
    {
        public DateTime Date { get; set; }
        public required string Type { get; set; }
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; }
        public string? Description { get; set; }
        public Guid? FileUploadId { get; set; }
    }
}
