using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class Account
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Portfolio Portfolio { get; set; } = null!;
        public required string AccountNumber { get; set; }
        public required string AccountName { get; set; }
        public required string Provider { get; set; }
        public required string ProductType { get; set; }
    }
}
