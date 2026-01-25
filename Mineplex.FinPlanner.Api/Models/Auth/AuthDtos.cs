using System.ComponentModel.DataAnnotations;

namespace Mineplex.FinPlanner.Api.Models.Auth
{
    public class RegisterRequest
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required, MinLength(8)]
        public required string Password { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class GoogleLoginRequest
    {
        [Required]
        public required string IdToken { get; set; }
    }

    public class AuthResponse
    {
        public required string Token { get; set; }
        public required UserDto User { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
    }
}
