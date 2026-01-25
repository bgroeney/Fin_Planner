using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Models.Auth;
using Google.Apis.Auth;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly FinPlannerDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(FinPlannerDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
                // In production, validate payload.Audience against your Client ID
            }
            catch (InvalidJwtException ex)
            {
                throw new Exception("Invalid Google Token: " + ex.Message);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
            if (user == null)
            {
                // Auto-register
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = payload.Email,
                    GoogleSubjectId = payload.Subject,
                    Role = "Owner",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else if (string.IsNullOrEmpty(user.GoogleSubjectId))
            {
                // Link account if existing but no Google ID
                user.GoogleSubjectId = payload.Subject;
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role
                }
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "super_secret_key_please_change_in_production_environment_1234567890";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "FinPlanner",
                audience: _configuration["Jwt:Audience"] ?? "FinPlannerUser",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
