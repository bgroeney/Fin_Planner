using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NetWorthController : ControllerBase
    {
        private readonly FinPlannerDbContext _context;

        public NetWorthController(FinPlannerDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<NetWorthSummary>> GetNetWorth(Guid portfolioId)
        {
            var assetsValue = await _context.Holdings
                .Where(h => h.Account.PortfolioId == portfolioId)
                .SumAsync(h => h.CurrentValue);

            var liabilitiesValue = await _context.Liabilities
                .Where(l => l.PortfolioId == portfolioId && !l.IsPaidOff)
                .SumAsync(l => l.PrincipalAmount);

            return new NetWorthSummary
            {
                TotalAssets = (double)assetsValue,
                TotalLiabilities = (double)liabilitiesValue,
                NetWorth = (double)(assetsValue - liabilitiesValue)
            };
        }
    }

    public class NetWorthSummary
    {
        public double TotalAssets { get; set; }
        public double TotalLiabilities { get; set; }
        public double NetWorth { get; set; }
    }
}
