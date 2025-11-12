using FirstLend.Domain.Abstractions;
using FirstLend.Domain.Dtos.Request;
using FirstLend.Domain.Dtos.Response;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using FirstLend.Infrastructure.Services.External;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace FirstLend.Infrastructure.Services
{
    public class KycService : IKycService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FirstLendDbContext _context;
        private readonly ILogger<KycService> _logger;
        private readonly ICreditScoreService _creditScoreService;

        public KycService(
            HttpClient httpClient,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            FirstLendDbContext context,
            ILogger<KycService> logger,
            ICreditScoreService creditScoreService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _creditScoreService = creditScoreService;
        }

        public async Task<KycVerificationResponse> VerifyKycAsync(KycVerificationRequest request, string userId)
        {
            try
            {
                // Get the user
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new KycVerificationResponse
                    {
                        Success = false,
                        Message = "User not found",
                        Code = "USER_NOT_FOUND"
                    };
                }

                // Call Mono CRC API
                var monoResponse = await CallMonoCrcApiAsync(request.BVN);

                if (monoResponse == null || monoResponse.Status != "successful")
                {
                    _logger.LogWarning($"Mono API call failed for user {userId}");
                    return new KycVerificationResponse
                    {
                        Success = false,
                        Message = "Failed to fetch credit history from Mono",
                        Code = "MONO_API_ERROR"
                    };
                }

                // Perform all verification checks
                var verificationData = new KycVerificationData
                {
                    FullName = monoResponse.Data.Profile.FullName,
                    DateOfBirth = monoResponse.Data.Profile.DateOfBirth,
                    Gender = monoResponse.Data.Profile.Gender,
                    PhoneNumbers = monoResponse.Data.Profile.PhoneNumber ?? new List<string>(),
                    EmailAddresses = monoResponse.Data.Profile.EmailAddress ?? new List<string>(),
                    AddressHistory = monoResponse.Data.Profile.AddressHistory?
                        .Select(ah => new AddressHistoryDto
                        {
                            Address = ah.Address,
                            Type = ah.Type,
                            DateReported = ah.DateReported
                        })
                        .ToList() ?? new List<AddressHistoryDto>()
                };

                // Verify BVN (mandatory - strict check)
                var bvnVerified = VerifyIdentification(
                    monoResponse.Data.Profile.Identifications,
                    "BVN",
                    request.BVN);

                verificationData.BvnVerification = new FieldVerificationDto
                {
                    IsMatched = bvnVerified,
                    ProvidedValue = request.BVN,
                    VerifiedValue = monoResponse.Data.Profile.Identifications
                        .FirstOrDefault(x => x.Type.Equals("BVN", StringComparison.OrdinalIgnoreCase))?.Number ?? "",
                    Message = bvnVerified ? "BVN verified successfully" : "BVN does not match"
                };

                // If BVN doesn't match, return failure
                if (!bvnVerified)
                {
                    return new KycVerificationResponse
                    {
                        Success = false,
                        Message = "BVN verification failed - provided BVN does not match records",
                        Code = "BVN_VERIFICATION_FAILED",
                        Data = verificationData
                    };
                }

                // Verify NIN if provided (strict check if provided)
                if (!string.IsNullOrWhiteSpace(request.NIN))
                {
                    var ninVerified = VerifyIdentification(
                        monoResponse.Data.Profile.Identifications,
                        "NIN",
                        request.NIN);

                    verificationData.NinVerification = new FieldVerificationDto
                    {
                        IsMatched = ninVerified,
                        ProvidedValue = request.NIN,
                        VerifiedValue = monoResponse.Data.Profile.Identifications
                            .FirstOrDefault(x => x.Type.Equals("NIN", StringComparison.OrdinalIgnoreCase))?.Number ?? "",
                        Message = ninVerified ? "NIN verified successfully" : "NIN does not match"
                    };

                    // NIN must match if provided - strict check
                    if (!ninVerified)
                    {
                        return new KycVerificationResponse
                        {
                            Success = false,
                            Message = "NIN verification failed - provided NIN does not match records",
                            Code = "NIN_VERIFICATION_FAILED",
                            Data = verificationData
                        };
                    }
                }
                else
                {
                    verificationData.NinVerification = new FieldVerificationDto
                    {
                        IsMatched = false,
                        ProvidedValue = "",
                        VerifiedValue = monoResponse.Data.Profile.Identifications
                            .FirstOrDefault(x => x.Type.Equals("NIN", StringComparison.OrdinalIgnoreCase))?.Number ?? "",
                        Message = "NIN was not provided for verification"
                    };
                }

                // Verify full name from logged-in user profile (strict check)
                var userFullName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(userFullName))
                {
                    var fullNameVerified = VerifyFullName(userFullName, monoResponse.Data.Profile.FullName);
                    verificationData.FullNameVerification = new FieldVerificationDto
                    {
                        IsMatched = fullNameVerified,
                        ProvidedValue = userFullName,
                        VerifiedValue = monoResponse.Data.Profile.FullName,
                        Message = fullNameVerified ? "Full name matches" : "Full name does not match"
                    };

                    // Full name must match - strict check
                    if (!fullNameVerified)
                    {
                        return new KycVerificationResponse
                        {
                            Success = false,
                            Message = "Full name verification failed - your profile full name does not match Mono records",
                            Code = "FULLNAME_VERIFICATION_FAILED",
                            Data = verificationData
                        };
                    }
                }
                else
                {
                    return new KycVerificationResponse
                    {
                        Success = false,
                        Message = "Cannot verify full name - user profile does not have a complete full name set",
                        Code = "PROFILE_INCOMPLETE"
                    };
                }

                // Update user with KYC verification
                user.KycVerified = true;
                user.BVN = request.BVN;
                user.NIN = request.NIN ?? "";
                user.KycVerificationDate = DateTime.UtcNow;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError($"Failed to update user KYC status: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                    return new KycVerificationResponse
                    {
                        Success = false,
                        Message = "Failed to save KYC verification",
                        Code = "SAVE_FAILED"
                    };
                }

                verificationData.IsVerified = true;

                // Assign 5 random credit accounts to the user for credit score calculation
                await _creditScoreService.AssignRandomCreditAccountsAsync(userId, 5);

                // Prepare response
                return new KycVerificationResponse
                {
                    Success = true,
                    Message = verificationData.Warnings.Count == 0
                        ? "KYC verification successful with all details matching"
                        : $"KYC verification successful with {verificationData.Warnings.Count} warning(s)",
                    Code = "KYC_VERIFIED",
                    Data = verificationData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in KYC verification: {ex.Message}", ex);
                return new KycVerificationResponse
                {
                    Success = false,
                    Message = "An error occurred during KYC verification",
                    Code = "INTERNAL_ERROR"
                };
            }
        }

        private async Task<MonoCrcResponse?> CallMonoCrcApiAsync(string bvn)
        {
            try
            {
                var monoSecKey = _configuration["Mono:SecretKey"];
                var baseUrl = _configuration["Mono:BaseUrl"];

                if (string.IsNullOrEmpty(monoSecKey) || string.IsNullOrEmpty(baseUrl))
                {
                    _logger.LogError("Mono API credentials not configured");
                    return null;
                }

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/lookup/credit-history/crc")
                {
                    Content = JsonContent.Create(new { bvn = bvn })
                };

                request.Headers.Add("mono-sec-key", monoSecKey);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Mono API returned status {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MonoCrcResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception calling Mono API: {ex.Message}", ex);
                return null;
            }
        }

        private bool VerifyIdentification(List<MonoIdentification> identifications, string type, string value)
        {
            if (identifications == null || identifications.Count == 0)
                return false;

            return identifications.Any(id =>
                id.Type.Equals(type, StringComparison.OrdinalIgnoreCase) &&
                id.Number.Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        private bool VerifyFullName(string? providedFullName, string verifiedFullName)
        {
            if (string.IsNullOrWhiteSpace(providedFullName) || string.IsNullOrWhiteSpace(verifiedFullName))
                return false;

            // Normalize names: remove extra spaces and convert to lowercase for comparison
            var normalized1 = System.Text.RegularExpressions.Regex.Replace(providedFullName.ToLower().Trim(), @"\s+", " ");
            var normalized2 = System.Text.RegularExpressions.Regex.Replace(verifiedFullName.ToLower().Trim(), @"\s+", " ");

            // Check for exact match or if one contains the other (to handle variations)
            return normalized1 == normalized2 || normalized1.Contains(normalized2) || normalized2.Contains(normalized1);
        }

        /// <summary>
        /// Get the KYC verification status of a user
        /// </summary>
        public async Task<KycStatusResponse> GetKycStatusAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new KycStatusResponse
                    {
                        Success = false,
                        Message = "User not found",
                        Code = "USER_NOT_FOUND"
                    };
                }

                var statusData = new KycStatusData
                {
                    IsVerified = user.KycVerified,
                    VerificationDate = user.KycVerificationDate,
                    BVN = user.BVN,
                    NIN = user.NIN,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };

                return new KycStatusResponse
                {
                    Success = true,
                    Message = user.KycVerified ? "User is KYC verified" : "User is not KYC verified",
                    Code = user.KycVerified ? "KYC_VERIFIED" : "KYC_NOT_VERIFIED",
                    Data = statusData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving KYC status for user {userId}: {ex.Message}");
                return new KycStatusResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving KYC status",
                    Code = "INTERNAL_ERROR"
                };
            }
        }

        /// <summary>
        /// Update the KYC verification status of a user (Admin function)
        /// </summary>
        public async Task<KycStatusResponse> UpdateKycStatusAsync(string userId, UpdateKycStatusRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new KycStatusResponse
                    {
                        Success = false,
                        Message = "User not found",
                        Code = "USER_NOT_FOUND"
                    };
                }

                // Update KYC status
                user.KycVerified = request.IsVerified;
                if (request.IsVerified)
                {
                    user.KycVerificationDate = DateTime.UtcNow;
                }
                else
                {
                    user.KycVerificationDate = null;
                    user.BVN = "";
                    user.NIN = "";
                    
                    // Remove credit accounts when KYC is unverified
                    var userCreditAccounts = _context.UserCreditAccounts.Where(uca => uca.UserId == userId);
                    _context.UserCreditAccounts.RemoveRange(userCreditAccounts);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Removed credit accounts for user {userId} due to KYC unverification");
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError($"Failed to update KYC status for user {userId}: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                    return new KycStatusResponse
                    {
                        Success = false,
                        Message = "Failed to update KYC status",
                        Code = "UPDATE_FAILED"
                    };
                }

                // If status is being set to verified, assign credit accounts
                if (request.IsVerified)
                {
                    try
                    {
                        await _creditScoreService.AssignRandomCreditAccountsAsync(userId, 5);
                        _logger.LogInformation($"Assigned 5 random credit accounts to user {userId} after manual KYC verification");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to assign credit accounts after manual KYC verification for user {userId}: {ex.Message}");
                        // Don't fail the entire operation if credit account assignment fails
                    }
                }

                var statusData = new KycStatusData
                {
                    IsVerified = user.KycVerified,
                    VerificationDate = user.KycVerificationDate,
                    BVN = user.BVN,
                    NIN = user.NIN,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };

                _logger.LogInformation($"KYC status updated for user {userId}. New status: {(request.IsVerified ? "Verified" : "Unverified")}. Reason: {request.Reason}");

                return new KycStatusResponse
                {
                    Success = true,
                    Message = request.IsVerified ? "User KYC status updated to verified" : "User KYC status updated to unverified",
                    Code = "STATUS_UPDATED",
                    Data = statusData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating KYC status for user {userId}: {ex.Message}");
                return new KycStatusResponse
                {
                    Success = false,
                    Message = "An error occurred while updating KYC status",
                    Code = "INTERNAL_ERROR"
                };
            }
        }
    }
}
