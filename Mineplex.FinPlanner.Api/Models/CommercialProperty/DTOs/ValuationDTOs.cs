using System;

namespace Mineplex.FinPlanner.Api.Models
{
    public class CreateValuationRequest
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public required string Source { get; set; }
        public string? Notes { get; set; }
    }
}
