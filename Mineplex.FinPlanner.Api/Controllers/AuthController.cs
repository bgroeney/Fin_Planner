using Microsoft.AspNetCore.Mvc;
using Mineplex.FinPlanner.Api.Models.Auth;
using Mineplex.FinPlanner.Api.Services;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.IdToken))
                    return BadRequest(new { message = "ID Token is required" });

                var response = await _authService.LoginWithGoogleAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
