using FirstLend.Application.Abstractions;
using FirstLend.Application.Dtos.Response;
using FirstLend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FirstLend.Api.Controllers
{
    [ApiController]
    [Route("api/payment-history")]
    [Authorize]
    public class PaymentHistoryController : ControllerBase
    {
        private readonly FirstLendDbContext _context;

        public PaymentHistoryController(FirstLendDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get payment history for the authenticated user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaymentHistory(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Try to get user ID from "sub" claim first, then fallback to NameIdentifier
                var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ServiceResponse<List<PaymentHistoryResponse>>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "401",
                        Data = null
                    });
                }

                var query = _context.PaymentHistories
                    .Include(ph => ph.Loan)
                    .Where(ph => ph.UserId == userId)
                    .AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(ph => ph.Status.ToLower() == status.ToLower());
                }

                // Search by loan ID or transaction ID
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(ph => 
                        ph.TransactionId.ToString().ToLower().Contains(searchLower) ||
                        ph.LoanId.ToString().ToLower().Contains(searchLower));
                }

                // Order by date descending
                query = query.OrderByDescending(ph => ph.CreatedAt);

                var totalCount = await query.CountAsync();
                var payments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paymentResponses = payments.Select(ph => new PaymentHistoryResponse
                {
                    Id = ph.Id,
                    TransactionId = ph.TransactionId.ToString(),
                    LoanId = ph.LoanId,
                    LoanReference = $"LN-{ph.LoanId.ToString().Substring(0, 8)}",
                    Amount = ph.Amount,
                    Principal = ph.Principal,
                    Interest = ph.Interest,
                    Method = ph.Method,
                    Status = ph.Status,
                    CreatedAt = DateTime.Parse(ph.CreatedAt)
                }).ToList();

                return Ok(new ServiceResponse<List<PaymentHistoryResponse>>
                {
                    Success = true,
                    Message = "Payment history retrieved successfully",
                    Code = "200",
                    Data = paymentResponses,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ServiceResponse<List<PaymentHistoryResponse>>
                {
                    Success = false,
                    Message = "Error retrieving payment history",
                    Code = "500",
                    Data = null,
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get payment history summary/statistics for the authenticated user
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetPaymentSummary()
        {
            try
            {
                // Try to get user ID from "sub" claim first, then fallback to NameIdentifier
                var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ServiceResponse<PaymentHistorySummaryResponse>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "401",
                        Data = null
                    });
                }

                var payments = await _context.PaymentHistories
                    .Where(ph => ph.UserId == userId)
                    .ToListAsync();

                var summary = new PaymentHistorySummaryResponse
                {
                    TotalPayments = payments.Count,
                    TotalAmountPaid = payments.Where(p => p.Status.ToLower() == "successful").Sum(p => p.Amount),
                    SuccessfulPayments = payments.Count(p => p.Status.ToLower() == "successful"),
                    FailedOrPendingPayments = payments.Count(p => p.Status.ToLower() != "successful")
                };

                return Ok(new ServiceResponse<PaymentHistorySummaryResponse>
                {
                    Success = true,
                    Message = "Payment summary retrieved successfully",
                    Code = "200",
                    Data = summary
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ServiceResponse<PaymentHistorySummaryResponse>
                {
                    Success = false,
                    Message = "Error retrieving payment summary",
                    Code = "500",
                    Data = null,
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get payment history for a specific loan
        /// </summary>
        [HttpGet("loan/{loanId}")]
        public async Task<IActionResult> GetLoanPaymentHistory(Guid loanId)
        {
            try
            {
                // Try to get user ID from "sub" claim first, then fallback to NameIdentifier
                var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ServiceResponse<List<PaymentHistoryResponse>>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "401",
                        Data = null
                    });
                }

                // Verify the loan belongs to the user
                var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == loanId && l.BorrowerId == userId);
                if (loan == null)
                {
                    return NotFound(new ServiceResponse<List<PaymentHistoryResponse>>
                    {
                        Success = false,
                        Message = "Loan not found",
                        Code = "404",
                        Data = null
                    });
                }

                var payments = await _context.PaymentHistories
                    .Where(ph => ph.LoanId == loanId)
                    .OrderByDescending(ph => ph.CreatedAt)
                    .ToListAsync();

                var paymentResponses = payments.Select(ph => new PaymentHistoryResponse
                {
                    Id = ph.Id,
                    TransactionId = ph.TransactionId.ToString(),
                    LoanId = ph.LoanId,
                    LoanReference = $"LN-{ph.LoanId.ToString().Substring(0, 8)}",
                    Amount = ph.Amount,
                    Principal = ph.Principal,
                    Interest = ph.Interest,
                    Method = ph.Method,
                    Status = ph.Status,
                    CreatedAt = DateTime.Parse(ph.CreatedAt)
                }).ToList();

                return Ok(new ServiceResponse<List<PaymentHistoryResponse>>
                {
                    Success = true,
                    Message = "Loan payment history retrieved successfully",
                    Code = "200",
                    Data = paymentResponses
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ServiceResponse<List<PaymentHistoryResponse>>
                {
                    Success = false,
                    Message = "Error retrieving loan payment history",
                    Code = "500",
                    Data = null,
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
