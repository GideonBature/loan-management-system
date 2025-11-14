using FirstLend.Application.Abstractions;
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
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin,Super Admin,Loan Officer,Auditor")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly FirstLendDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGeminiService _geminiService;

        public AdminDashboardController(
            FirstLendDbContext context, 
            UserManager<ApplicationUser> userManager,
            IGeminiService geminiService)
        {
            _context = context;
            _userManager = userManager;
            _geminiService = geminiService;
        }    /// <summary>
    /// Get dashboard summary statistics with date filters (Admin only)
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ServiceResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary([FromQuery] string period = "month")
    {
        try
        {
            // Calculate date range based on period
            DateTime startDate;
            DateTime endDate = DateTime.UtcNow;

            switch (period.ToLower())
            {
                case "today":
                    startDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
                    break;
                case "week":
                case "7days":
                    startDate = DateTime.UtcNow.AddDays(-7);
                    break;
                case "month":
                default:
                    startDate = DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), DateTimeKind.Utc);
                    break;
            }

            // Total New Applications (within period)
            var totalNewApplications = await _context.Loans
                .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .CountAsync();

            // Total Disbursed (active + completed loans within period)
            var totalDisbursed = await _context.Loans
                .Where(l => (l.Status == LoanStatus.active || l.Status == LoanStatus.completed) 
                    && l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .SumAsync(l => (decimal?)l.Principal) ?? 0;

            // Total Outstanding (sum of AmountDue for active loans)
            var totalOutstanding = await _context.Loans
                .Where(l => l.Status == LoanStatus.active)
                .SumAsync(l => (decimal?)l.AmountDue) ?? 0;

            // Loan Application Status counts
            var pendingLoans = await _context.Loans
                .Where(l => l.Status == LoanStatus.pending && l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .CountAsync();

            // Approved includes both 'approved' and 'active' statuses (all approved loans whether disbursed or not)
            var approvedLoans = await _context.Loans
                .Where(l => (l.Status == LoanStatus.approved || l.Status == LoanStatus.active) 
                    && l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .CountAsync();

            var rejectedLoans = await _context.Loans
                .Where(l => l.Status == LoanStatus.rejected && l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .CountAsync();

            var underReviewLoans = await _context.Loans
                .Where(l => l.Status == LoanStatus.pending && l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .CountAsync();

            // Loan Type Distribution
            var loanTypeDistribution = await _context.Loans
                .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .Include(l => l.LoanType)
                .GroupBy(l => l.LoanType!.Name)
                .Select(g => new { LoanType = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalLoansForDistribution = loanTypeDistribution.Sum(x => x.Count);
            var loanTypeStats = loanTypeDistribution.Select(x => new
            {
                LoanType = x.LoanType,
                Count = x.Count,
                Percentage = totalLoansForDistribution > 0 ? Math.Round((double)x.Count / totalLoansForDistribution * 100, 1) : 0
            }).ToList();

            // Monthly Disbursement Trend (last 12 months)
            var monthlyDisbursements = new List<object>();
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                var amount = await _context.Loans
                    .Where(l => (l.Status == LoanStatus.active || l.Status == LoanStatus.completed)
                        && l.CreatedAt >= monthStart && l.CreatedAt <= monthEnd)
                    .SumAsync(l => (decimal?)l.Principal) ?? 0;

                monthlyDisbursements.Add(new
                {
                    Month = monthStart.ToString("MMM"),
                    Amount = amount
                });
            }

            // Calculate percentage changes (comparing to previous period)
            DateTime previousStartDate;
            switch (period.ToLower())
            {
                case "today":
                    previousStartDate = new DateTime(DateTime.UtcNow.AddDays(-1).Year, DateTime.UtcNow.AddDays(-1).Month, DateTime.UtcNow.AddDays(-1).Day, 0, 0, 0, DateTimeKind.Utc);
                    break;
                case "week":
                case "7days":
                    previousStartDate = DateTime.UtcNow.AddDays(-14);
                    break;
                case "month":
                default:
                    previousStartDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
                    break;
            }

            var previousApplications = await _context.Loans
                .Where(l => l.CreatedAt >= previousStartDate && l.CreatedAt < startDate)
                .CountAsync();

            var previousDisbursed = await _context.Loans
                .Where(l => (l.Status == LoanStatus.active || l.Status == LoanStatus.completed)
                    && l.CreatedAt >= previousStartDate && l.CreatedAt < startDate)
                .SumAsync(l => (decimal?)l.Principal) ?? 0;

            var applicationsChange = previousApplications > 0 
                ? Math.Round((double)(totalNewApplications - previousApplications) / previousApplications * 100, 1)
                : 0;

            var disbursedChange = previousDisbursed > 0
                ? Math.Round((double)(totalDisbursed - previousDisbursed) / (double)previousDisbursed * 100, 1)
                : 0;

            var response = new
            {
                Period = period,
                DateRange = new { Start = startDate, End = endDate },
                
                // Summary Cards
                TotalNewApplications = totalNewApplications,
                ApplicationsChangePercentage = applicationsChange,
                
                TotalDisbursed = totalDisbursed,
                DisbursedChangePercentage = disbursedChange,
                
                TotalOutstanding = totalOutstanding,
                OutstandingChangePercentage = 5.4, // You can calculate this similarly
                
                // Loan Application Status
                LoanApplicationStatus = new
                {
                    Pending = pendingLoans,
                    Approved = approvedLoans,
                    Rejected = rejectedLoans,
                    UnderReview = underReviewLoans
                },
                
                // Loan Type Distribution
                LoanTypeDistribution = loanTypeStats,
                
                // Monthly Disbursement Trend
                MonthlyDisbursementTrend = monthlyDisbursements
            };

            return Ok(new ServiceResponse<object>
            {
                Success = true,
                Message = "Dashboard summary retrieved successfully",
                Code = "200",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<object>
            {
                Success = false,
                Message = "Error retrieving dashboard summary",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get dashboard summary statistics (DEPRECATED - use /summary with period param)
    /// </summary>
    [HttpGet("summary-old")]
    public async Task<IActionResult> GetDashboardSummaryOld()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalLoans = await _context.Loans.CountAsync();
            var totalLoanTypes = await _context.LoanTypes.CountAsync();

            var pendingLoans = await _context.Loans
                .Where(l => l.Status == LoanStatus.pending)
                .CountAsync();

            var approvedLoans = await _context.Loans
                .Where(l => l.Status == LoanStatus.approved)
                .CountAsync();

            var activeLoans = await _context.Loans
                .Where(l => l.Status == LoanStatus.active)
                .CountAsync();

            var totalLoanAmount = await _context.Loans
                .SumAsync(l => (decimal?)l.Principal) ?? 0;

            var totalAmountDisbursed = await _context.Loans
                .Where(l => l.Status == LoanStatus.active || l.Status == LoanStatus.completed)
                .SumAsync(l => (decimal?)l.Principal) ?? 0;

            var summary = new DashboardSummaryResponse
            {
                TotalLoans = totalLoans,
                PendingLoans = pendingLoans,
                ApprovedLoans = approvedLoans,
                RejectedLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.rejected),
                TotalLoanAmount = totalLoanAmount,
                DisbursedAmount = totalAmountDisbursed,
                TotalCustomers = totalUsers,
                ActiveCustomers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active),
                AverageMonthlyIncome = await _context.Users
                    .Where(u => u.UserType == UserType.Customer)
                    .AverageAsync(u => (decimal?)(u.Address != null ? 0 : 0)) ?? 0
            };

            return Ok(new ServiceResponse<DashboardSummaryResponse>
            {
                Success = true,
                Message = "Dashboard summary retrieved successfully",
                Code = "200",
                Data = summary
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<DashboardSummaryResponse>
            {
                Success = false,
                Message = "Error retrieving dashboard summary",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get loan statistics breakdown (Admin only)
    /// </summary>
    [HttpGet("loans/stats")]
    public async Task<IActionResult> GetLoanStats()
    {
        try
        {
            var pendingCount = await _context.Loans.CountAsync(l => l.Status == LoanStatus.pending);
            var approvedCount = await _context.Loans.CountAsync(l => l.Status == LoanStatus.approved);
            var activeCount = await _context.Loans.CountAsync(l => l.Status == LoanStatus.active);
            var completedCount = await _context.Loans.CountAsync(l => l.Status == LoanStatus.completed);
            var rejectedCount = await _context.Loans.CountAsync(l => l.Status == LoanStatus.rejected);
            var defaultedCount = await _context.Loans.CountAsync(l => l.Status == LoanStatus.defaulted);
            
            var totalLoans = await _context.Loans.CountAsync();
            var totalPrincipal = await _context.Loans.SumAsync(l => (decimal?)l.Principal) ?? 0;
            var averageAmount = totalLoans > 0 ? totalPrincipal / totalLoans : 0;
            var averageRate = await _context.Loans
                .Where(l => l.Rate > 0)
                .AverageAsync(l => (double?)(l.Rate)) ?? 0;

            var stats = new LoanStatisticsResponse
            {
                TotalLoans = totalLoans,
                PendingCount = pendingCount,
                ApprovedCount = approvedCount,
                ActiveCount = activeCount,
                CompletedCount = completedCount,
                RejectedCount = rejectedCount,
                DefaultedCount = defaultedCount,
                TotalPrincipal = totalPrincipal,
                AverageAmount = averageAmount,
                AverageRate = averageRate
            };

            return Ok(new ServiceResponse<LoanStatisticsResponse>
            {
                Success = true,
                Message = "Loan statistics retrieved successfully",
                Code = "200",
                Data = stats
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<LoanStatisticsResponse>
            {
                Success = false,
                Message = "Error retrieving loan statistics",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get user statistics breakdown (Admin only)
    /// </summary>
    [HttpGet("users/stats")]
    public async Task<IActionResult> GetUserStats()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active);
            var inactiveUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Inactive);
            var suspendedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Suspended);

            var usersWithLoans = await _context.Loans
                .Select(l => l.BorrowerId)
                .Distinct()
                .CountAsync();

            var usersWithoutLoans = totalUsers - usersWithLoans;

            var totalBorrowedAmount = await _context.Loans.SumAsync(l => (decimal?)l.Principal) ?? 0;
            var averageMonthlyIncome = await _context.Users
                .Where(u => u.UserType == UserType.Customer)
                .CountAsync() > 0 
                ? await _context.Loans
                    .Where(l => l.MonthlyIncome > 0)
                    .AverageAsync(l => (decimal?)l.MonthlyIncome) ?? 0
                : 0;

            var stats = new UserStatisticsResponse
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                SuspendedUsers = suspendedUsers,
                UsersWithLoans = usersWithLoans,
                UsersWithoutLoans = usersWithoutLoans,
                AverageMonthlyIncome = averageMonthlyIncome,
                TotalBorrowedAmount = totalBorrowedAmount
            };

            return Ok(new ServiceResponse<UserStatisticsResponse>
            {
                Success = true,
                Message = "User statistics retrieved successfully",
                Code = "200",
                Data = stats
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<UserStatisticsResponse>
            {
                Success = false,
                Message = "Error retrieving user statistics",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get AI-powered insights (Admin only)
    /// </summary>
    [HttpGet("ai-insights")]
    public async Task<IActionResult> GetAiInsights([FromQuery] string mode = "HighConfidence")
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            
            // Get current month data
            var totalApplications = await _context.Loans
                .CountAsync(l => l.CreatedAt >= startOfMonth);
            
            var totalDisbursed = await _context.Loans
                .Where(l => l.DisbursedAt >= startOfMonth && l.Status == LoanStatus.active)
                .SumAsync(l => l.Principal);
            
            var totalOutstanding = await _context.Loans
                .Where(l => l.Status == LoanStatus.active)
                .SumAsync(l => l.AmountDue);

            var approvedLoans = await _context.Loans
                .CountAsync(l => l.CreatedAt >= startOfMonth && 
                                (l.Status == LoanStatus.approved || l.Status == LoanStatus.active));

            var rejectedLoans = await _context.Loans
                .CountAsync(l => l.CreatedAt >= startOfMonth && l.Status == LoanStatus.rejected);

            var overdueLoans = await _context.Loans
                .CountAsync(l => l.Status == LoanStatus.active && l.DueAt < now);

            var approvalRate = totalApplications > 0 ? (double)approvedLoans / totalApplications * 100 : 0;

            // Get previous month data for comparison
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var lastMonthApplications = await _context.Loans
                .CountAsync(l => l.CreatedAt >= startOfLastMonth && l.CreatedAt < startOfMonth);
            
            var lastMonthDisbursed = await _context.Loans
                .Where(l => l.DisbursedAt >= startOfLastMonth && 
                           l.DisbursedAt < startOfMonth && 
                           l.Status == LoanStatus.active)
                .SumAsync(l => l.Principal);

            var growthRate = lastMonthApplications > 0
                ? ((totalApplications - lastMonthApplications) * 100.0 / lastMonthApplications)
                : 0;

            // Prepare comprehensive data context for Gemini
            string dataContext = $@"
FirstLend Loan Management System - Financial Analysis Data

CURRENT MONTH ({now:MMMM yyyy}):
- Total Applications: {totalApplications}
- Total Disbursed: ₦{totalDisbursed:N2}
- Total Outstanding: ₦{totalOutstanding:N2}
- Approved Loans: {approvedLoans}
- Rejected Loans: {rejectedLoans}
- Overdue Loans: {overdueLoans}
- Approval Rate: {approvalRate:F1}%

PREVIOUS MONTH COMPARISON:
- Last Month Applications: {lastMonthApplications}
- Last Month Disbursed: ₦{lastMonthDisbursed:N2}
- Growth Rate: {growthRate:F1}%

KEY METRICS:
- Risk Level: {(totalOutstanding > totalDisbursed * 0.9m ? "HIGH" : totalOutstanding > totalDisbursed * 0.7m ? "MODERATE" : "LOW")}
- Utilization Rate: {(totalDisbursed > 0 ? (totalOutstanding / totalDisbursed * 100) : 0):F1}%
- Default Risk: {(approvedLoans > 0 ? ((double)overdueLoans / approvedLoans * 100) : 0):F1}%
";

            string prompt;
            var tags = new List<string>();
            var metrics = new Dictionary<string, object>();

            if (mode == "PredictiveAnalytics")
            {
                // Predictive Analytics Mode
                prompt = $@"You are FirstLend's AI Financial Analyst. Analyze the data and provide a PREDICTIVE insight.

{dataContext}

Generate ONE concise paragraph (2-3 sentences, max 60 words) that:
1. Forecasts next month's trends based on growth rate
2. Predicts potential risks or opportunities
3. Provides actionable recommendations

Start with: 'Based on current trends, loan applications are expected to...'
Be specific with numbers and confident in predictions.";

                tags.Add("Predictive Analytics");
                
                var predictedApplications = (int)(totalApplications * (1 + growthRate / 100));
                var predictedDisbursement = totalDisbursed * (decimal)(1 + growthRate / 100);
                
                metrics["predictedApplications"] = predictedApplications;
                metrics["predictedDisbursement"] = predictedDisbursement;
                metrics["trendDirection"] = growthRate > 0 ? "upward" : "downward";
                metrics["confidence"] = Math.Min(95, 70 + Math.Abs(growthRate));
                metrics["growthRate"] = Math.Round(growthRate, 1);
            }
            else
            {
                // High Confidence Mode
                prompt = $@"You are FirstLend's AI Financial Analyst. Analyze the data and provide a HIGH-CONFIDENCE insight.

{dataContext}

Generate ONE concise paragraph (2-3 sentences, max 60 words) that:
1. Summarizes current month's operational performance
2. Assesses risk based on outstanding vs disbursed amounts
3. Evaluates approval rate and provides recommendations

Start with: 'Loan operations processed {totalApplications} new applications this month...'
Be factual, data-driven, and include specific numbers.";

                tags.Add("High Confidence");
                
                var riskLevel = totalOutstanding > totalDisbursed * 0.9m ? "high" : 
                               totalOutstanding > totalDisbursed * 0.7m ? "moderate" : "low";
                
                metrics["riskLevel"] = riskLevel;
                metrics["approvalRate"] = Math.Round(approvalRate, 1);
                metrics["utilizationRate"] = totalDisbursed > 0 ? Math.Round((double)(totalOutstanding / totalDisbursed * 100), 1) : 0;
                metrics["defaultRisk"] = approvedLoans > 0 ? Math.Round((double)overdueLoans / approvedLoans * 100, 1) : 0;
                metrics["totalApplications"] = totalApplications;
                metrics["totalDisbursed"] = totalDisbursed;
                metrics["totalOutstanding"] = totalOutstanding;
            }

            // Call Gemini AI to generate insight
            var geminiInsight = await _geminiService.AnalyzeLoanDataAsync(prompt);

            // Fallback if Gemini fails
            if (string.IsNullOrWhiteSpace(geminiInsight))
            {
                if (mode == "PredictiveAnalytics")
                {
                    var predictedApplications = (int)(totalApplications * (1 + growthRate / 100));
                    geminiInsight = $"Based on current trends, loan applications are expected to {(growthRate > 0 ? "increase" : "decrease")} by {Math.Abs(Math.Round(growthRate, 1))}% next month. " +
                                  $"Projected: {predictedApplications} applications with ₦{(totalDisbursed * (decimal)(1 + growthRate / 100)):N0} disbursement.";
                }
                else
                {
                    var riskLevel = totalOutstanding > totalDisbursed * 0.9m ? "high" : "moderate";
                    geminiInsight = $"Loan operations processed {totalApplications} new applications this month, disbursing ₦{totalDisbursed:N0} " +
                                  $"while keeping outstanding exposure at ₦{totalOutstanding:N0}. Risk level: {riskLevel}.";
                }
            }

            var response = new AiInsightsResponse
            {
                Insight = geminiInsight,
                Mode = mode,
                Tags = tags,
                Metrics = metrics
            };

            return Ok(new ServiceResponse<AiInsightsResponse>
            {
                Success = true,
                Message = "AI insights generated successfully",
                Code = "200",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<AiInsightsResponse>
            {
                Success = false,
                Message = "Error generating AI insights",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }
}
