using KupeServer.Api.Features.Auth.Dtos;
using KupeServer.Api.Features.Users.Dtos;
using KupeServer.Api.Features.Users.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KupeServer.Api.Features.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(AuthService authService, ExternalAuthService externalAuthService) : ControllerBase    {
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(RegisterDto request)
        {
            var user = await authService.Register(request);
            var response = new UserResponseDto(user);

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginDto request)
        {
            var result  = await authService.Login(request);

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshToken(request);

            return Ok(result);
        }

        [HttpPost("external/{provider}")]
        public async Task<ActionResult<TokenResponseDto>> ExternalLogin(
            string provider, ExternalLoginRequestDto request, CancellationToken ct)
        {
            var result = await externalAuthService.Login(provider, request.Token, ct);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("external/{provider}/link")]
        public async Task<IActionResult> LinkExternal(
            string provider, ExternalLoginRequestDto request, CancellationToken ct)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await externalAuthService.Link(userId, provider, request.Token, ct);

            return NoContent();
        }

        [Authorize]
        [HttpGet]
        public ActionResult<string> GetSecret()
        {
            return Ok("You are authenticated");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public ActionResult<string> AdminEndpoint()
        {
            return Ok("You are an admin");
        }
    }
}
