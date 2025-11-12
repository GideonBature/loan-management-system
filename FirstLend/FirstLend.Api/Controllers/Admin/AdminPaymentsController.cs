using FirstLend.Application.Dtos.Response;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstLend.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/payments")]
    [Authorize(Roles = "Admin")]
    public class AdminPaymentsController : ControllerBase
    {
        private readonly FirstLendDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminPaymentsController(FirstLendDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get all payment history for admin (Admin only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.PaymentHistories
                    .Include(ph => ph.Loan)
                    .AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
                {
                    query = query.Where(ph => ph.Status.ToLower() == status.ToLower());
                }

                // Search by customer name, loan ID, or payment ID
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(ph => 
                        ph.Id.ToString().ToLower().Contains(searchLower) ||
                        ph.LoanId.ToString().ToLower().Contains(searchLower) ||
                        ph.TransactionId.ToString().ToLower().Contains(searchLower));
                }

                // Filter by date range
                if (startDate.HasValue)
                {
                    query = query.Where(ph => DateTime.Parse(ph.CreatedAt) >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(ph => DateTime.Parse(ph.CreatedAt) <= endDate.Value.AddDays(1));
                }

                // Order by date descending
                query = query.OrderByDescending(ph => ph.CreatedAt);

                var totalCount = await query.CountAsync();
                var payments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paymentResponses = new List<AdminPaymentResponse>();

                foreach (var payment in payments)
                {
                    var user = await _userManager.FindByIdAsync(payment.UserId);
                    var customerName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";

                    paymentResponses.Add(new AdminPaymentResponse
                    {
                        PaymentId = $"PID-{payment.Id.ToString().Substring(0, 3).ToUpper()}",
                        CustomerName = customerName,
                        LoanId = $"LID-{payment.LoanId.ToString().Substring(0, 3).ToUpper()}",
                        DueDate = payment.Loan?.DueAt ?? DateTime.UtcNow,
                        Amount = payment.Amount,
                        Status = payment.Status,
                        CreatedAt = DateTime.Parse(payment.CreatedAt)
                    });
                }

                return Ok(new ServiceResponse<List<AdminPaymentResponse>>
                {
                    Success = true,
                    Message = "Payments retrieved successfully",
                    Code = "200",
                    Data = paymentResponses,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ServiceResponse<List<AdminPaymentResponse>>
                {
                    Success = false,
                    Message = "Error retrieving payments",
                    Code = "500",
                    Data = null,
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get payment statistics for admin dashboard
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetPaymentStats()
        {
            try
            {
                var totalPayments = await _context.PaymentHistories.CountAsync();
                var successfulPayments = await _context.PaymentHistories
                    .Where(ph => ph.Status.ToLower() == "success" || ph.Status.ToLower() == "successful")
                    .CountAsync();
                var pendingPayments = await _context.PaymentHistories
                    .Where(ph => ph.Status.ToLower() == "pending")
                    .CountAsync();
                var failedPayments = await _context.PaymentHistories
                    .Where(ph => ph.Status.ToLower() == "failed" || ph.Status.ToLower() == "declined")
                    .CountAsync();
                var totalAmountReceived = await _context.PaymentHistories
                    .Where(ph => ph.Status.ToLower() == "success" || ph.Status.ToLower() == "successful")
                    .SumAsync(ph => ph.Amount);

                return Ok(new ServiceResponse<object>
                {
                    Success = true,
                    Message = "Payment statistics retrieved successfully",
                    Code = "200",
                    Data = new
                    {
                        TotalPayments = totalPayments,
                        SuccessfulPayments = successfulPayments,
                        PendingPayments = pendingPayments,
                        FailedPayments = failedPayments,
                        TotalAmountReceived = totalAmountReceived
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ServiceResponse<object>
                {
                    Success = false,
                    Message = "Error retrieving payment statistics",
                    Code = "500",
                    Data = null,
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
