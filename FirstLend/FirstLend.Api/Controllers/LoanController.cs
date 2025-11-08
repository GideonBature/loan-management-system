using FirstLend.Application.Abstractions;
using FirstLend.Application.Dtos.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FirstLend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoanController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoanController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    /// <summary>
    /// Create a new loan application
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                code = "400",
                data = (object?)null,
                errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "User not authenticated",
                code = "401",
                data = (object?)null,
                errors = new[] { "Invalid user token" }
            });
        }

        try
        {
            var response = await _loanService.CreateAsync(userId, request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(
                nameof(GetLoanById), 
                new { id = response.Data?.Id }, 
                response
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing your loan application",
                code = "500",
                data = (object?)null,
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get loan by ID (borrower can view own loans, admin can view all)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoanById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "User not authenticated",
                code = 401,
                data = (object?)null,
                errors = new[] { "Invalid user token" }
            });
        }

        var response = await _loanService.GetByIdAsync(id, userId);
        
        if (!response.Success)
        {
            return response.Code == "404" ? NotFound(response) : Unauthorized(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get all loans for the current user
    /// </summary>
    [HttpGet("my-loans")]
    public async Task<IActionResult> GetMyLoans([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "User not authenticated",
                code = 401,
                data = (object?)null,
                errors = new[] { "Invalid user token" }
            });
        }

        var response = await _loanService.GetUserLoansAsync(userId, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get all loans (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllLoans([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var response = await _loanService.GetAllLoansAsync(page, pageSize);
        return Ok(response);
    }
}
