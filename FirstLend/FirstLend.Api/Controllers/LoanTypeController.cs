using FirstLend.Application.Abstractions;
using FirstLend.Application.Dtos.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstLend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanTypeController : ControllerBase
{
    private readonly ILoanTypeService _loanTypeService;

    public LoanTypeController(ILoanTypeService loanTypeService)
    {
        _loanTypeService = loanTypeService;
    }

    /// <summary>
    /// Create a new loan type (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateLoanType([FromBody] CreateLoanTypeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                code = 400,
                data = (object?)null,
                errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
            });
        }

        var response = await _loanTypeService.CreateAsync(request);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetLoanTypeById), 
            new { id = response.Data?.Id }, 
            response
        );
    }

    /// <summary>
    /// Get loan type by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLoanTypeById(Guid id)
    {
        var response = await _loanTypeService.GetByIdAsync(id);
        
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get all loan types with pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllLoanTypes(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        [FromQuery] bool? activeOnly = true)
    {
        var response = await _loanTypeService.GetAllAsync(page, pageSize, activeOnly);
        return Ok(response);
    }

    /// <summary>
    /// Update a loan type (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateLoanType(Guid id, [FromBody] UpdateLoanTypeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                code = 400,
                data = (object?)null,
                errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
            });
        }

        var response = await _loanTypeService.UpdateAsync(id, request);
        
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete a loan type (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLoanType(Guid id)
    {
        var response = await _loanTypeService.DeleteAsync(id);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Toggle loan type status (Activate/Deactivate) - Admin only
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleLoanTypeStatus(Guid id)
    {
        var response = await _loanTypeService.ToggleStatusAsync(id);
        
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }
}
