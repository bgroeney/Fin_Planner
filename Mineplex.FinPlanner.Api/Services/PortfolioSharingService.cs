using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IPortfolioSharingService
    {
        Task<List<PortfolioShareDto>> GetSharesForPortfolioAsync(Guid ownerId, Guid portfolioId);
        Task<PortfolioShareDto> SharePortfolioAsync(Guid ownerId, Guid portfolioId, SharePortfolioRequest request);
        Task RevokeShareAsync(Guid ownerId, Guid shareId);
        Task<List<Guid>> GetSharedPortfolioIdsAsync(Guid userId);
    }

    public class PortfolioSharingService : IPortfolioSharingService
    {
        private readonly FinPlannerDbContext _context;
        private readonly ILogger<PortfolioSharingService> _logger;

        public PortfolioSharingService(FinPlannerDbContext context, ILogger<PortfolioSharingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PortfolioShareDto>> GetSharesForPortfolioAsync(Guid ownerId, Guid portfolioId)
        {
            // Verify ownership
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == ownerId);
            if (portfolio == null)
            {
                throw new KeyNotFoundException("Portfolio not found or access denied");
            }

            return await _context.PortfolioShares
                .Where(ps => ps.PortfolioId == portfolioId)
                .Include(ps => ps.SharedWithUser)
                .Select(ps => new PortfolioShareDto
                {
                    Id = ps.Id,
                    SharedWithUserId = ps.SharedWithUserId,
                    SharedWithUserEmail = ps.SharedWithUser.Email,
                    SharedWithUserName = ps.SharedWithUser.Email, // Using email as display name
                    Role = ps.Role,
                    CreatedAt = ps.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<PortfolioShareDto> SharePortfolioAsync(Guid ownerId, Guid portfolioId, SharePortfolioRequest request)
        {
            // Verify ownership
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == ownerId);
            if (portfolio == null)
            {
                throw new KeyNotFoundException("Portfolio not found or access denied");
            }

            // Find user by email
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (targetUser == null)
            {
                throw new KeyNotFoundException($"User with email '{request.Email}' not found");
            }

            // Can't share with yourself
            if (targetUser.Id == ownerId)
            {
                throw new InvalidOperationException("Cannot share portfolio with yourself");
            }

            // Check if already shared
            var existingShare = await _context.PortfolioShares
                .FirstOrDefaultAsync(ps => ps.PortfolioId == portfolioId && ps.SharedWithUserId == targetUser.Id);

            if (existingShare != null)
            {
                throw new InvalidOperationException($"Portfolio already shared with {request.Email}");
            }

            // Validate role
            var validRoles = new[] { "Viewer", "Editor", "Admin" };
            if (!validRoles.Contains(request.Role))
            {
                throw new ArgumentException($"Invalid role. Must be one of: {string.Join(", ", validRoles)}");
            }

            var share = new PortfolioShare
            {
                Id = Guid.NewGuid(),
                PortfolioId = portfolioId,
                SharedWithUserId = targetUser.Id,
                Role = request.Role,
                CreatedAt = DateTime.UtcNow,
                InvitedByUserId = ownerId
            };

            _context.PortfolioShares.Add(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Portfolio {PortfolioId} shared with user {UserId} as {Role}", portfolioId, targetUser.Id, request.Role);

            return new PortfolioShareDto
            {
                Id = share.Id,
                SharedWithUserId = targetUser.Id,
                SharedWithUserEmail = targetUser.Email,
                SharedWithUserName = targetUser.Email, // Using email as display name
                Role = share.Role,
                CreatedAt = share.CreatedAt
            };
        }

        public async Task RevokeShareAsync(Guid ownerId, Guid shareId)
        {
            var share = await _context.PortfolioShares
                .Include(ps => ps.Portfolio)
                .FirstOrDefaultAsync(ps => ps.Id == shareId);

            if (share == null)
            {
                throw new KeyNotFoundException("Share not found");
            }

            // Only owner can revoke
            if (share.Portfolio.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("Only the portfolio owner can revoke shares");
            }

            _context.PortfolioShares.Remove(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Share {ShareId} revoked for portfolio {PortfolioId}", shareId, share.PortfolioId);
        }

        public async Task<List<Guid>> GetSharedPortfolioIdsAsync(Guid userId)
        {
            return await _context.PortfolioShares
                .Where(ps => ps.SharedWithUserId == userId)
                .Select(ps => ps.PortfolioId)
                .ToListAsync();
        }
    }

    public class PortfolioShareDto
    {
        public Guid Id { get; set; }
        public Guid SharedWithUserId { get; set; }
        public string SharedWithUserEmail { get; set; } = string.Empty;
        public string SharedWithUserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SharePortfolioRequest
    {
        public required string Email { get; set; }
        public string Role { get; set; } = "Viewer";
    }
}
