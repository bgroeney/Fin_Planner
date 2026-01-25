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
    public class AccountsController : ControllerBase
    {
        private readonly FinPlannerDbContext _context;

        public AccountsController(FinPlannerDbContext context)
        {
            _context = context;
        }

        [HttpGet("portfolio/{portfolioId}")]
        public async Task<ActionResult<List<Account>>> GetAccounts(Guid portfolioId)
        {
            return await _context.Accounts.Where(a => a.PortfolioId == portfolioId).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccount(Account account)
        {
            account.Id = Guid.NewGuid();
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return Ok(account);
        }
    }
}
