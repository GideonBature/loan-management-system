using FirstLend.Domain.Abstractions;
using FirstLend.Domain.Dtos.Request;
using FirstLend.Domain.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FirstLend.Api.Controllers
{
    [ApiController]
    [Route("api/kyc")]
    [Authorize]
    public class KycController : ControllerBase
    {
        private readonly IKycService _kycService;
        private readonly ILogger<KycController> _logger;

        public KycController(IKycService kycService, ILogger<KycController> logger)
        {
            _kycService = kycService;
            _logger = logger;
        }

        /// <summary>
        /// Verify KYC with BVN and NIN
        /// </summary>
        /// <param name="request">KYC verification request containing BVN and NIN</param>
        /// <returns>KYC verification result</returns>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(KycVerificationResponse), 200)]
        [ProducesResponseType(typeof(KycVerificationResponse), 400)]
        [ProducesResponseType(typeof(KycVerificationResponse), 401)]
        [ProducesResponseType(typeof(KycVerificationResponse), 500)]
        public async Task<IActionResult> VerifyKyc([FromBody] KycVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new KycVerificationResponse
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Code = "VALIDATION_ERROR"
                    });
                }

                // Get current user ID from JWT token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new KycVerificationResponse
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "UNAUTHORIZED"
                    });
                }

                _logger.LogInformation($"KYC verification request for user: {userId}");

                var result = await _kycService.VerifyKycAsync(request, userId);

                if (result.Success)
                {
                    return Ok(result);
                }

                if (result.Code == "USER_NOT_FOUND")
                {
                    return NotFound(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during KYC verification: {ex.Message}", ex);
                return StatusCode(500, new KycVerificationResponse
                {
                    Success = false,
                    Message = "An error occurred during KYC verification",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get the current KYC verification status
        /// </summary>
        /// <returns>KYC verification status</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(KycStatusResponse), 200)]
        [ProducesResponseType(typeof(KycStatusResponse), 401)]
        [ProducesResponseType(typeof(KycStatusResponse), 404)]
        [ProducesResponseType(typeof(KycStatusResponse), 500)]
        public async Task<IActionResult> GetKycStatus()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new KycStatusResponse
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "UNAUTHORIZED"
                    });
                }

                _logger.LogInformation($"Retrieving KYC status for user: {userId}");

                var result = await _kycService.GetKycStatusAsync(userId);

                if (!result.Success && result.Code == "USER_NOT_FOUND")
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception retrieving KYC status: {ex.Message}", ex);
                return StatusCode(500, new KycStatusResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving KYC status",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Update KYC verification status (Admin only)
        /// </summary>
        /// <param name="request">Update request with new verification status</param>
        /// <returns>Updated KYC status</returns>
        [HttpPut("status")]
        [ProducesResponseType(typeof(KycStatusResponse), 200)]
        [ProducesResponseType(typeof(KycStatusResponse), 400)]
        [ProducesResponseType(typeof(KycStatusResponse), 401)]
        [ProducesResponseType(typeof(KycStatusResponse), 403)]
        [ProducesResponseType(typeof(KycStatusResponse), 404)]
        [ProducesResponseType(typeof(KycStatusResponse), 500)]
        public async Task<IActionResult> UpdateKycStatus([FromBody] UpdateKycStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new KycStatusResponse
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Code = "VALIDATION_ERROR"
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new KycStatusResponse
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "UNAUTHORIZED"
                    });
                }

                _logger.LogInformation($"Updating KYC status for user: {userId}. New status: {request.IsVerified}");

                var result = await _kycService.UpdateKycStatusAsync(userId, request);

                if (!result.Success && result.Code == "USER_NOT_FOUND")
                {
                    return NotFound(result);
                }

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception updating KYC status: {ex.Message}", ex);
                return StatusCode(500, new KycStatusResponse
                {
                    Success = false,
                    Message = "An error occurred while updating KYC status",
                    Code = "INTERNAL_ERROR"
                });
            }
        }
    }
}
