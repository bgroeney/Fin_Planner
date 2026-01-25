using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AssetsController : ControllerBase
    {
        private readonly FinPlannerDbContext _context;

        public AssetsController(FinPlannerDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Asset>>> SearchAssets([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Asset>();

            // Case insensitive search
            return await _context.Assets
                .Where(a => EF.Functions.ILike(a.Symbol, $"%{query}%") || EF.Functions.ILike(a.Name, $"%{query}%"))
                .Take(10)
                .ToListAsync();
        }
    }
}
