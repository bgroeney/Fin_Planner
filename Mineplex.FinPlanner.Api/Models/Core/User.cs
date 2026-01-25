using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public string? GoogleSubjectId { get; set; }
        public required string Role { get; set; } // Admin, Editor
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
