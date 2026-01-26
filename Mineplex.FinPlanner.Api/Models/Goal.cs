using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mineplex.FinPlanner.Api.Models
{
    public class Goal
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal TargetAmount { get; set; }

        public decimal CurrentAmount { get; set; }

        [Required]
        public DateTime TargetDate { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for User if we have a User model, but using ID reference for now as Auth handling is generic
    }
}
