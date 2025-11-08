using FirstLend.Domain.Abstractions;
using FirstLend.Domain.Dtos.Request;
using FirstLend.Domain.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstLend.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), 201)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 409)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Validation error",
                    Code = "VALIDATION_ERROR",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => new ValidationError
                        {
                            Field = "general",
                            Message = e.ErrorMessage
                        })
                        .ToList()
                });
            }

            var result = await _authService.RegisterAsync(request);
            if (result.Success)
            {
                return Created("", result);
            }

            if (result.Code == "EMAIL_EXISTS" || result.Code == "PHONE_EXISTS")
            {
                return Conflict(result);
            }

            return BadRequest(result);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Validation error",
                    Code = "VALIDATION_ERROR"
                });
            }

            var result = await _authService.LoginAsync(request);
            if (result.Success)
            {
                return Ok(result);
            }

            return Unauthorized(result);
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 401)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Validation error",
                    Code = "VALIDATION_ERROR"
                });
            }

            var result = await _authService.RefreshTokenAsync(request);
            if (result.Success)
            {
                return Ok(result);
            }

            return Unauthorized(result);
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token",
                    Code = "INVALID_TOKEN"
                });
            }

            var result = await _authService.LogoutAsync(userId);
            return Ok(result);
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Validation error",
                    Code = "VALIDATION_ERROR"
                });
            }

            var result = await _authService.ForgotPasswordAsync(request);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Validation error",
                    Code = "VALIDATION_ERROR"
                });
            }

            var result = await _authService.ResetPasswordAsync(request);
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token",
                    Code = "INVALID_TOKEN"
                });
            }

            var user = await _authService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                return NotFound(new AuthResponse
                {
                    Success = false,
                    Message = "User not found",
                    Code = "USER_NOT_FOUND"
                });
            }

            return Ok(user);
        }
    }
}