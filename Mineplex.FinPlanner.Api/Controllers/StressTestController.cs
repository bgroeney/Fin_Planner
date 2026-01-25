using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mineplex.FinPlanner.Api.Services;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StressTestController : ControllerBase
    {
        private readonly IStressTestEngine _stressTestEngine;

        public StressTestController(IStressTestEngine stressTestEngine)
        {
            _stressTestEngine = stressTestEngine;
        }

        [HttpPost("simulate")]
        public async Task<ActionResult<StressTestResult>> RunSimulation([FromBody] StressTestParameters parameters, [FromQuery] Guid portfolioId)
        {
            var result = await _stressTestEngine.RunSimulationAsync(portfolioId, parameters);
            return Ok(result);
        }
    }
}
