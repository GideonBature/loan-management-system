using FirstLend.Application.Abstractions;
using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;
using FirstLend.Domain.Enums;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstLend.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/loans")]
[Authorize(Roles = "Admin")]
public class AdminLoansController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly FirstLendDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminLoansController(
        ILoanService loanService, 
        FirstLendDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _loanService = loanService;
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Get all loans with optional status filter, search by applicant name/ID, and sorting (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllLoans(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "date",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var query = _context.Loans
                .Include(l => l.LoanType)
                .AsQueryable();

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<LoanStatus>(status, ignoreCase: true, out var loanStatus))
                {
                    query = query.Where(l => l.Status == loanStatus);
                }
            }

            // Apply sorting before fetching borrower details
            if (sortBy?.ToLower() == "amount")
            {
                query = sortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(l => l.Principal)
                    : query.OrderByDescending(l => l.Principal);
            }
            else
            {
                query = sortOrder?.ToLower() == "asc"
                    ? query.OrderBy(l => l.CreatedAt)
                    : query.OrderByDescending(l => l.CreatedAt);
            }

            var totalCount = await query.CountAsync();
            var loans = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var loanResponses = new List<AdminLoanResponse>();
            foreach (var loan in loans)
            {
                var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);
                
                // Apply search filter on borrower name/email/ID
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    var borrowerName = borrower?.UserName ?? "Unknown";
                    var borrowerEmail = borrower?.Email ?? "Unknown";
                    var borrowerId = loan.BorrowerId;
                    
                    if (!borrowerName.ToLower().Contains(searchLower) &&
                        !borrowerEmail.ToLower().Contains(searchLower) &&
                        !borrowerId.ToLower().Contains(searchLower))
                    {
                        continue;
                    }
                }

                loanResponses.Add(new AdminLoanResponse
                {
                    Id = loan.Id,
                    BorrowerName = borrower?.UserName ?? "Unknown",
                    BorrowerEmail = borrower?.Email ?? "Unknown",
                    BorrowerId = loan.BorrowerId,
                    Principal = loan.Principal,
                    AmountDue = loan.AmountDue,
                    LoanTypeName = loan.LoanType?.Name ?? "",
                    Rate = loan.Rate,
                    Term = loan.Term,
                    EmploymentStatus = loan.EmploymentStatus,
                    MonthlyIncome = loan.MonthlyIncome,
                    Purpose = loan.Purpose,
                    Status = loan.Status,
                    CreatedAt = loan.CreatedAt,
                    NextPaymentDate = loan.NextPaymentDate,
                    DueAt = loan.DueAt
                });
            }

            // Recalculate total count if search filter was applied
            var finalCount = string.IsNullOrEmpty(search) ? totalCount : loanResponses.Count;

            return Ok(new ServiceResponse<List<AdminLoanResponse>>
            {
                Success = true,
                Message = "Loans retrieved successfully",
                Code = "200",
                Data = loanResponses,
                TotalCount = finalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<List<AdminLoanResponse>>
            {
                Success = false,
                Message = "Error retrieving loans",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get loan details by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoanDetails(Guid id)
    {
        try
        {
            var loan = await _context.Loans
                .Include(l => l.LoanType)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                return NotFound(new ServiceResponse<AdminLoanResponse>
                {
                    Success = false,
                    Message = "Loan not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "The requested loan does not exist" }
                });
            }

            var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);

            var response = new AdminLoanResponse
            {
                Id = loan.Id,
                BorrowerName = borrower?.UserName ?? "Unknown",
                BorrowerEmail = borrower?.Email ?? "Unknown",
                Principal = loan.Principal,
                AmountDue = loan.AmountDue,
                LoanTypeName = loan.LoanType?.Name ?? "",
                Rate = loan.Rate,
                Term = loan.Term,
                EmploymentStatus = loan.EmploymentStatus,
                MonthlyIncome = loan.MonthlyIncome,
                Purpose = loan.Purpose,
                Status = loan.Status,
                CreatedAt = loan.CreatedAt,
                NextPaymentDate = loan.NextPaymentDate,
                DueAt = loan.DueAt
            };

            return Ok(new ServiceResponse<AdminLoanResponse>
            {
                Success = true,
                Message = "Loan details retrieved successfully",
                Code = "200",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<AdminLoanResponse>
            {
                Success = false,
                Message = "Error retrieving loan details",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Approve a loan application (Admin only)
    /// </summary>
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveLoan(Guid id)
    {
        try
        {
            var loan = await _context.Loans
                .Include(l => l.LoanType)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                return NotFound(new ServiceResponse<AdminLoanResponse>
                {
                    Success = false,
                    Message = "Loan not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "The requested loan does not exist" }
                });
            }

            if (loan.Status != LoanStatus.pending)
            {
                return BadRequest(new ServiceResponse<AdminLoanResponse>
                {
                    Success = false,
                    Message = "Cannot approve loan",
                    Code = "400",
                    Data = null,
                    Errors = new[] { $"Only pending loans can be approved. Current status: {loan.Status}" }
                });
            }

            loan.Status = LoanStatus.approved;
            loan.ApprovedAt = DateTime.UtcNow;
            _context.Loans.Update(loan);
            await _context.SaveChangesAsync();

            var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);

            var response = new AdminLoanResponse
            {
                Id = loan.Id,
                BorrowerName = borrower?.UserName ?? "Unknown",
                BorrowerEmail = borrower?.Email ?? "Unknown",
                Principal = loan.Principal,
                AmountDue = loan.AmountDue,
                LoanTypeName = loan.LoanType?.Name ?? "",
                Rate = loan.Rate,
                Term = loan.Term,
                EmploymentStatus = loan.EmploymentStatus,
                MonthlyIncome = loan.MonthlyIncome,
                Purpose = loan.Purpose,
                Status = loan.Status,
                CreatedAt = loan.CreatedAt,
                NextPaymentDate = loan.NextPaymentDate,
                DueAt = loan.DueAt
            };

            return Ok(new ServiceResponse<AdminLoanResponse>
            {
                Success = true,
                Message = "Loan approved successfully",
                Code = "200",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<AdminLoanResponse>
            {
                Success = false,
                Message = "Error approving loan",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Reject a loan application (Admin only)
    /// </summary>
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectLoan(Guid id, [FromBody] LoanActionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AdminLoanResponse>
                {
                    Success = false,
                    Message = "Validation failed",
                    Code = "400",
                    Data = null,
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()
                });
            }

            var loan = await _context.Loans
                .Include(l => l.LoanType)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                return NotFound(new ServiceResponse<AdminLoanResponse>
                {
                    Success = false,
                    Message = "Loan not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "The requested loan does not exist" }
                });
            }

            if (loan.Status != LoanStatus.pending)
            {
                return BadRequest(new ServiceResponse<AdminLoanResponse>
                {
                    Success = false,
                    Message = "Cannot reject loan",
                    Code = "400",
                    Data = null,
                    Errors = new[] { $"Only pending loans can be rejected. Current status: {loan.Status}" }
                });
            }

            loan.Status = LoanStatus.rejected;
            _context.Loans.Update(loan);
            await _context.SaveChangesAsync();

            var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);

            var response = new AdminLoanResponse
            {
                Id = loan.Id,
                BorrowerName = borrower?.UserName ?? "Unknown",
                BorrowerEmail = borrower?.Email ?? "Unknown",
                Principal = loan.Principal,
                AmountDue = loan.AmountDue,
                LoanTypeName = loan.LoanType?.Name ?? "",
                Rate = loan.Rate,
                Term = loan.Term,
                EmploymentStatus = loan.EmploymentStatus,
                MonthlyIncome = loan.MonthlyIncome,
                Purpose = loan.Purpose,
                Status = loan.Status,
                CreatedAt = loan.CreatedAt,
                NextPaymentDate = loan.NextPaymentDate,
                DueAt = loan.DueAt
            };

            return Ok(new ServiceResponse<AdminLoanResponse>
            {
                Success = true,
                Message = "Loan rejected successfully",
                Code = "200",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<AdminLoanResponse>
            {
                Success = false,
                Message = "Error rejecting loan",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }
}

