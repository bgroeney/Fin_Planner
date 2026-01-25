using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class DecisionLog
    {
        public Guid Id { get; set; }
        public Guid DecisionId { get; set; }
        public Guid UserId { get; set; }
        public required string Action { get; set; } // Create, Update, StatusChange, Delete
        public string Details { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
