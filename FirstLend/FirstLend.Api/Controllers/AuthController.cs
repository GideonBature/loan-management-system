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
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
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
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Extract userId - the 'sub' claim is converted to NameIdentifier by ASP.NET
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token - no user ID found",
                        code = "NO_USER_ID",
                        data = (object?)null,
                        errors = new[] { "Unable to extract user ID from authentication token" }
                    });
                }

                var user = await _authService.GetCurrentUserAsync(userId);
                
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found",
                        code = "USER_NOT_FOUND",
                        data = (object?)null,
                        errors = new[] { $"No user found with ID: {userId}" }
                    });
                }
                
                return Ok(new
                {
                    success = true,
                    message = "User retrieved successfully",
                    code = "",
                    data = user,
                    errors = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetCurrentUser");
                
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    code = "INTERNAL_ERROR",
                    data = (object?)null,
                    errors = new[] { ex.Message }
                });
            }
        }
    }
}