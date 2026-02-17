using System;
using System.ComponentModel.DataAnnotations;

namespace Mineplex.FinPlanner.Api.Models
{
    public class PortfolioShare
    {
        public Guid Id { get; set; }

        public Guid PortfolioId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Portfolio Portfolio { get; set; } = null!;

        public Guid SharedWithUserId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public User SharedWithUser { get; set; } = null!;

        /// <summary>
        /// Role: Viewer, Editor, Admin
        /// </summary>
        [Required]
        public string Role { get; set; } = "Viewer";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? InvitedByUserId { get; set; }
    }
}
