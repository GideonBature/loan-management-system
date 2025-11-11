using FirstLend.Domain.Abstractions;
using FirstLend.Domain.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FirstLend.Api.Controllers
{
    [ApiController]
    [Route("api/credit-score")]
    [Authorize]
    public class CreditScoreController : ControllerBase
    {
        private readonly ICreditScoreService _creditScoreService;
        private readonly ILogger<CreditScoreController> _logger;

        public CreditScoreController(ICreditScoreService creditScoreService, ILogger<CreditScoreController> logger)
        {
            _creditScoreService = creditScoreService;
            _logger = logger;
        }

        /// <summary>
        /// Get the credit score of the currently logged-in user
        /// </summary>
        /// <returns>Credit score details with breakdown</returns>
        [HttpGet]
        [ProducesResponseType(typeof(CreditScoreResponse), 200)]
        [ProducesResponseType(typeof(CreditScoreResponse), 401)]
        [ProducesResponseType(typeof(CreditScoreResponse), 404)]
        [ProducesResponseType(typeof(CreditScoreResponse), 500)]
        public async Task<IActionResult> GetCreditScore()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new CreditScoreResponse
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "UNAUTHORIZED"
                    });
                }

                _logger.LogInformation($"Fetching credit score for user: {userId}");

                var result = await _creditScoreService.GetUserCreditScoreAsync(userId);

                if (!result.Success && result.Code == "USER_NOT_FOUND")
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception fetching credit score: {ex.Message}", ex);
                return StatusCode(500, new CreditScoreResponse
                {
                    Success = false,
                    Message = "An error occurred while fetching credit score",
                    Code = "INTERNAL_ERROR"
                });
            }
        }
    }
}
