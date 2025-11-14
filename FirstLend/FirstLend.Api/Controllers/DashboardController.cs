using FirstLend.Application.Abstractions;
using FirstLend.Domain.Abstractions;
using FirstLend.Domain.Enums;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FirstLend.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Customer")]
public class DashboardController : ControllerBase
{
    private readonly FirstLendDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGeminiService _geminiService;
    private readonly ICreditScoreService _creditScoreService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        FirstLendDbContext context,
        UserManager<ApplicationUser> userManager,
        IGeminiService geminiService,
        ICreditScoreService creditScoreService,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _userManager = userManager;
        _geminiService = geminiService;
        _creditScoreService = creditScoreService;
        _logger = logger;
    }

    /// <summary>
    /// Get AI-powered personalized financial insights for the customer
    /// </summary>
    [HttpGet("ai-insights")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAIInsights()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Get user's active loans
            var activeLoans = await _context.Loans
                .Where(l => l.BorrowerId == userId && l.Status == LoanStatus.active)
                .ToListAsync();

            // Get user's loan history
            var loanHistory = await _context.Loans
                .Where(l => l.BorrowerId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Get user's payment history
            var paymentHistory = await _context.PaymentHistories
                .Where(p => activeLoans.Select(l => l.Id).Contains(p.LoanId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Get user's credit score
            var creditScoreResponse = await _creditScoreService.GetUserCreditScoreAsync(userId);
            var creditScore = creditScoreResponse.Success ? creditScoreResponse.Data?.Score ?? 0 : 0;
            var creditRating = creditScoreResponse.Success ? creditScoreResponse.Data?.Rating ?? "Unknown" : "Unknown";

            // Calculate financial metrics
            var totalBorrowed = loanHistory.Sum(l => l.Principal);
            var totalOutstanding = activeLoans.Sum(l => l.OutstandingBalance);
            var totalPaid = paymentHistory.Where(p => p.Status == "Success").Sum(p => p.Amount);
            var onTimePayments = paymentHistory.Count(p => p.Status == "Success");
            var latePayments = paymentHistory.Count(p => p.Status == "Failed" || p.Status == "Pending");
            var totalPayments = paymentHistory.Count;
            var paymentSuccessRate = totalPayments > 0 
                ? (double)onTimePayments / totalPayments * 100 
                : 0;

            // Calculate average monthly payment
            var avgMonthlyPayment = paymentHistory.Any() 
                ? (decimal)paymentHistory.Where(p => p.Status == "Success").Average(p => (decimal)p.Amount) 
                : 0m;

            // Get next payment due
            var nextPaymentDue = activeLoans
                .Where(l => l.NextPaymentDate > DateTime.MinValue)
                .OrderBy(l => l.NextPaymentDate)
                .FirstOrDefault()?.NextPaymentDate;

            // Count values for string interpolation
            var totalLoansCount = loanHistory.Count;
            var activeLoansCount = activeLoans.Count;
            var totalPaymentsCount = paymentHistory.Count;
            var totalCreditAccounts = creditScoreResponse.Data?.TotalAccounts ?? 0;

            // Build comprehensive prompt for Gemini
            var prompt = $@"You are a financial advisor AI for FirstLend, a loan management platform. Provide personalized financial insights and advice for this customer.

CUSTOMER PROFILE:
- Name: {user.FirstName} {user.LastName}
- Email: {user.Email}
- KYC Status: {(user.KycVerified ? "Verified" : "Not Verified")}
- Account Created: {user.CreatedAt:yyyy-MM-dd}

CREDIT PROFILE:
- Credit Score: {creditScore:F0}/100
- Credit Rating: {creditRating}
- Total Credit Accounts: {totalCreditAccounts}

LOAN HISTORY:
- Total Loans Applied: {totalLoansCount}
- Active Loans: {activeLoansCount}
- Total Amount Borrowed (All Time): ₦{totalBorrowed:N2}
- Current Outstanding Balance: ₦{totalOutstanding:N2}

PAYMENT HISTORY:
- Total Payments Made: {totalPaymentsCount}
- On-Time Payments: {onTimePayments}
- Late/Failed Payments: {latePayments}
- Payment Success Rate: {paymentSuccessRate:F1}%
- Total Amount Paid: ₦{totalPaid:N2}
- Average Monthly Payment: ₦{avgMonthlyPayment:N2}
- Next Payment Due: {(nextPaymentDue != null && nextPaymentDue.Value > DateTime.MinValue ? nextPaymentDue.Value.ToString("yyyy-MM-dd") : "No upcoming payments")}

TASK:
Based on this customer's actual financial data, provide:
1. A personalized insight about their current financial health (2-3 sentences)
2. One specific actionable recommendation to improve their credit score or financial situation
3. A prediction or encouraging statement about their financial future if they continue their current trajectory

Keep the response conversational, encouraging, and specific to their actual data. Focus on actionable advice.
Response format: Plain text, 3-4 sentences maximum, friendly and professional tone.";

            var aiInsight = await _geminiService.AnalyzeLoanDataAsync(prompt);

            return Ok(new
            {
                success = true,
                data = new
                {
                    insight = aiInsight,
                    metrics = new
                    {
                        creditScore,
                        creditRating,
                        activeLoans = activeLoansCount,
                        totalOutstanding,
                        paymentSuccessRate = Math.Round(paymentSuccessRate, 1),
                        nextPaymentDue = nextPaymentDue?.ToString("yyyy-MM-dd")
                    },
                    generatedAt = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI insights for customer dashboard");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while generating insights",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get customer dashboard summary statistics
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDashboardSummary()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Get user's loans
            var loans = await _context.Loans
                .Where(l => l.BorrowerId == userId)
                .ToListAsync();

            var activeLoans = loans.Where(l => l.Status == LoanStatus.active).ToList();

            // Get payment history
            var payments = await _context.PaymentHistories
                .Where(p => loans.Select(l => l.Id).Contains(p.LoanId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Get credit score
            var creditScoreResponse = await _creditScoreService.GetUserCreditScoreAsync(userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    user = new
                    {
                        name = $"{user.FirstName} {user.LastName}",
                        email = user.Email,
                        kycVerified = user.KycVerified,
                        emailVerified = user.EmailVerified
                    },
                    creditScore = creditScoreResponse.Success ? creditScoreResponse.Data : null,
                    loans = new
                    {
                        total = loans.Count,
                        active = activeLoans.Count,
                        totalBorrowed = loans.Sum(l => l.Principal),
                        totalOutstanding = activeLoans.Sum(l => l.OutstandingBalance)
                    },
                    payments = new
                    {
                        total = payments.Count,
                        successful = payments.Count(p => p.Status == "Success"),
                        totalPaid = payments.Where(p => p.Status == "Success").Sum(p => p.Amount),
                        nextDue = activeLoans
                            .Where(l => l.NextPaymentDate > DateTime.MinValue)
                            .OrderBy(l => l.NextPaymentDate)
                            .FirstOrDefault()?.NextPaymentDate
                    },
                    recentTransactions = payments.Take(5).Select(p => new
                    {
                        date = p.CreatedAt,
                        amount = p.Amount,
                        status = p.Status,
                        type = "Payment"
                    })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer dashboard summary");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while fetching dashboard data",
                error = ex.Message
            });
        }
    }
}
