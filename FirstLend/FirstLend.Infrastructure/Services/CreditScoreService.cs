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
        public async Task AssignRandomCreditAccountsAsync(string userId, int numberOfAccounts = 5)
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

                // Get user details to check for special handling
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError($"User {userId} not found");
                    return;
                }

                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                var isSamuelOlamide = fullName.Equals("Samuel Olamide", StringComparison.OrdinalIgnoreCase);

                // Get all available credit accounts with repayment history
                var allAccounts = await _context.CreditAccounts
                    .Include(ca => ca.RepaymentHistory)
                    .ToListAsync();

                if (allAccounts.Count == 0)
                {
                    _logger.LogWarning("No credit accounts available in the database for assignment");
                    return;
                }

                List<CreditAccount> selectedAccounts = new List<CreditAccount>();
                var random = new Random();

                if (isSamuelOlamide)
                {
                    // Special handling for Samuel Olamide - find accounts that give at least 72% score
                    _logger.LogInformation($"Special account selection for {fullName} - ensuring minimum 72% credit score");

                    const int maxAttempts = 100;
                    int attempts = 0;
                    double bestScore = 0;
                    List<CreditAccount> bestAccounts = new List<CreditAccount>();

                    // Try multiple random combinations to find one with score >= 72%
                    while (attempts < maxAttempts)
                    {
                        var candidateAccounts = allAccounts
                            .OrderBy(x => random.Next())
                            .Take(numberOfAccounts)
                            .ToList();

                        // Calculate what the credit score would be with these accounts
                        var testBreakdown = _scoreEngine.CalculateScore(candidateAccounts);

                        if (testBreakdown.TotalScore >= 72.0)
                        {
                            selectedAccounts = candidateAccounts;
                            _logger.LogInformation($"Found suitable accounts for {fullName} with score {testBreakdown.TotalScore}% on attempt {attempts + 1}");
                            break;
                        }

                        // Keep track of the best score found so far
                        if (testBreakdown.TotalScore > bestScore)
                        {
                            bestScore = testBreakdown.TotalScore;
                            bestAccounts = candidateAccounts;
                        }

                        attempts++;
                    }

                    // If we couldn't find accounts with 72%+, use the best we found
                    if (attempts >= maxAttempts)
                    {
                        selectedAccounts = bestAccounts;
                        _logger.LogWarning($"Could not find accounts with 72%+ for {fullName} after {maxAttempts} attempts. Best score found: {bestScore}%. Using those accounts.");
                    }
                }
                else
                {
                    // Normal random selection for other users
                    selectedAccounts = allAccounts
                        .OrderBy(x => random.Next())
                        .Take(numberOfAccounts)
                        .ToList();
                }

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
