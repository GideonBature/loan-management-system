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
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly FirstLendDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminDashboardController(FirstLendDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Get dashboard summary statistics (Admin only)
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary()
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
}
