using FirstLend.Domain.Abstractions;
using FirstLend.Domain.Dtos.Response;
using FirstLend.Domain.Entities;
using FirstLend.Domain.Models;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using FirstLend.Application.Behaviour;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirstLend.Infrastructure.Services
{
    public class CreditScoreService : ICreditScoreService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FirstLendDbContext _context;
        private readonly ILogger<CreditScoreService> _logger;
        private readonly CreditScoreEngine _scoreEngine;

        public CreditScoreService(
            UserManager<ApplicationUser> userManager,
            FirstLendDbContext context,
            ILogger<CreditScoreService> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _scoreEngine = new CreditScoreEngine();
        }

        /// <summary>
        /// Get the credit score for a user based on their assigned credit accounts
        /// </summary>
        public async Task<CreditScoreResponse> GetUserCreditScoreAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new CreditScoreResponse
                    {
                        Success = false,
                        Message = "User not found",
                        Code = "USER_NOT_FOUND"
                    };
                }

                // Get user's credit accounts
                var userAccountIds = await _context.UserCreditAccounts
                    .Where(uca => uca.UserId == userId)
                    .Select(uca => uca.CreditAccountId)
                    .ToListAsync();

                // If no accounts, return default score of 50
                if (!userAccountIds.Any())
                {
                    return new CreditScoreResponse
                    {
                        Success = true,
                        Message = "No credit history available - default score applied",
                        Code = "DEFAULT_SCORE",
                        Data = new CreditScoreData
                        {
                            Score = 50.0,
                            Rating = GetRating(50.0),
                            TotalAccounts = 0,
                            Breakdown = new ScoreBreakdown
                            {
                                PaymentHistoryScore = 50.0,
                                AmountsOwedScore = 50.0,
                                LengthOfHistoryScore = 50.0,
                                CreditMixScore = 50.0,
                                NewCreditScore = 50.0,
                                TotalScore = 50.0
                            },
                            CalculatedAt = DateTime.UtcNow
                        }
                    };
                }

                // Fetch the actual credit accounts with repayment history
                var creditAccounts = await _context.CreditAccounts
                    .Where(ca => userAccountIds.Contains(ca.Id))
                    .Include(ca => ca.RepaymentHistory)
                    .ToListAsync();

                // Calculate credit score using the engine
                var breakdown = _scoreEngine.CalculateScore(creditAccounts);

                // Special handling for Samuel Olamide - ensure minimum 80% credit score
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (fullName.Equals("Samuel Olamide", StringComparison.OrdinalIgnoreCase))
                {
                    if (breakdown.TotalScore < 80.0)
                    {
                        _logger.LogInformation($"Adjusting credit score for {fullName} from {breakdown.TotalScore} to 80.0 (minimum threshold)");
                        
                        // Boost the score to 80% minimum
                        breakdown.TotalScore = 80.0;
                        
                        // Also adjust individual components proportionally to reach 80%
                        var scaleFactor = 80.0 / (breakdown.PaymentHistoryScore + breakdown.AmountsOwedScore + 
                                                   breakdown.LengthOfHistoryScore + breakdown.CreditMixScore + 
                                                   breakdown.NewCreditScore);
                        
                        breakdown.PaymentHistoryScore = Math.Min(breakdown.PaymentHistoryScore * scaleFactor, 35.0);
                        breakdown.AmountsOwedScore = Math.Min(breakdown.AmountsOwedScore * scaleFactor, 30.0);
                        breakdown.LengthOfHistoryScore = Math.Min(breakdown.LengthOfHistoryScore * scaleFactor, 15.0);
                        breakdown.CreditMixScore = Math.Min(breakdown.CreditMixScore * scaleFactor, 10.0);
                        breakdown.NewCreditScore = Math.Min(breakdown.NewCreditScore * scaleFactor, 10.0);
                    }
                }

                return new CreditScoreResponse
                {
                    Success = true,
                    Message = "Credit score calculated successfully",
                    Code = "SCORE_CALCULATED",
                    Data = new CreditScoreData
                    {
                        Score = breakdown.TotalScore,
                        Rating = GetRating(breakdown.TotalScore),
                        TotalAccounts = creditAccounts.Count,
                        Breakdown = breakdown,
                        CalculatedAt = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating credit score for user {userId}: {ex.Message}");
                return new CreditScoreResponse
                {
                    Success = false,
                    Message = "An error occurred while calculating credit score",
                    Code = "INTERNAL_ERROR"
                };
            }
        }

        /// <summary>
        /// Assign random credit accounts to a user (called after KYC verification)
        /// </summary>
        public async Task AssignRandomCreditAccountsAsync(string userId, int numberOfAccounts = 3)
        {
            try
            {
                // Check if user already has accounts assigned
                var existingAccounts = await _context.UserCreditAccounts
                    .Where(uca => uca.UserId == userId)
                    .CountAsync();

                if (existingAccounts > 0)
                {
                    _logger.LogInformation($"User {userId} already has {existingAccounts} credit accounts assigned");
                    return;
                }

                // Get all available credit accounts
                var allAccounts = await _context.CreditAccounts.ToListAsync();

                if (allAccounts.Count == 0)
                {
                    _logger.LogWarning("No credit accounts available in the database for assignment");
                    return;
                }

                // Randomly select accounts
                var random = new Random();
                var selectedAccounts = allAccounts
                    .OrderBy(x => random.Next())
                    .Take(numberOfAccounts)
                    .ToList();

                // Create UserCreditAccount entries
                var userCreditAccounts = selectedAccounts.Select(account => new UserCreditAccount
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreditAccountId = account.Id
                }).ToList();

                // Save to database
                await _context.UserCreditAccounts.AddRangeAsync(userCreditAccounts);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Assigned {selectedAccounts.Count} random credit accounts to user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error assigning credit accounts to user {userId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get credit rating based on score
        /// </summary>
        private string GetRating(double score)
        {
            if (score >= 80) return "Excellent";
            if (score >= 70) return "Very Good";
            if (score >= 60) return "Good";
            if (score >= 50) return "Fair";
            return "Poor";
        }
    }
}
