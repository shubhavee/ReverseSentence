using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReverseSentence.DTOs;
using ReverseSentence.Services;

namespace ReverseSentence.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly ILogger<AuthController> logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            this.authService = authService;
            this.logger = logger;
        }

        /// <summary>
        /// Authenticate user and receive JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token for authentication</returns>
        [HttpPost("login")]
        [EnableRateLimiting("auth-limit")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await authService.AuthenticateAsync(request.Username, request.Password);

            if (result == null)
            {
                return Unauthorized(new { error = "Invalid username or password" });
            }

            return Ok(result);
        }
    }
}
