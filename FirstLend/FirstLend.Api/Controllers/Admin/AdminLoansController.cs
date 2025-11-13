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
    private readonly IConfiguration _configuration;

    public AdminLoansController(
        ILoanService loanService, 
        FirstLendDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _loanService = loanService;
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Generate account number from user ID (format: 309XXXXXX)
    /// </summary>
    private string GenerateAccountNumber(string userId)
    {
        var numericPart = userId.Replace("-", "").Substring(0, Math.Min(9, userId.Replace("-", "").Length));
        if (numericPart.Length >= 9)
        {
            return "309" + numericPart.Substring(0, 1) + "XXXXX";
        }
        return "3091XXXXX";
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

    /// <summary>
    /// Initialize disbursement with Paystack payment (Admin only)
    /// </summary>
    [HttpPost("{id}/initiate-disbursement")]
    public async Task<IActionResult> InitiateDisbursement(Guid id)
    {
        try
        {
            var loan = await _context.Loans
                .Include(l => l.LoanType)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                return NotFound(new ServiceResponse<object>
                {
                    Success = false,
                    Message = "Loan not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "The requested loan does not exist" }
                });
            }

            if (loan.Status != LoanStatus.approved)
            {
                return BadRequest(new ServiceResponse<object>
                {
                    Success = false,
                    Message = "Cannot disburse loan",
                    Code = "400",
                    Data = null,
                    Errors = new[] { $"Only approved loans can be disbursed. Current status: {loan.Status}" }
                });
            }

            var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);
            if (borrower == null)
            {
                return NotFound(new ServiceResponse<object>
                {
                    Success = false,
                    Message = "Borrower not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "Borrower account not found" }
                });
            }

            // Initialize Paystack payment for disbursement
            var paystackSecretKey = _configuration["Paystack:SecretKey"];
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {paystackSecretKey}");

            var callbackUrl = $"{_configuration["Frontend:BaseUrl"]}/admin/disbursement/callback";
            
            var paystackRequest = new
            {
                email = borrower.Email,
                amount = (int)(loan.Principal * 100), // Convert to kobo
                currency = "NGN",
                reference = $"DISB-{loan.Id}-{Guid.NewGuid().ToString().Substring(0, 8)}",
                callback_url = callbackUrl,
                metadata = new
                {
                    loan_id = loan.Id.ToString(),
                    borrower_id = loan.BorrowerId,
                    borrower_name = $"{borrower.FirstName} {borrower.LastName}",
                    disbursement = true
                }
            };

            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(paystackRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync("https://api.paystack.co/transaction/initialize", jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paystackResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseString);
                var authorizationUrl = paystackResponse.GetProperty("data").GetProperty("authorization_url").GetString();
                var reference = paystackResponse.GetProperty("data").GetProperty("reference").GetString();

                return Ok(new ServiceResponse<object>
                {
                    Success = true,
                    Message = "Disbursement payment initialized",
                    Code = "200",
                    Data = new
                    {
                        AuthorizationUrl = authorizationUrl,
                        Reference = reference,
                        Amount = loan.Principal
                    }
                });
            }

            return BadRequest(new ServiceResponse<object>
            {
                Success = false,
                Message = "Failed to initialize disbursement payment",
                Code = "400",
                Data = null,
                Errors = new[] { responseString }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ServiceResponse<object>
            {
                Success = false,
                Message = "Error initiating disbursement",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Verify disbursement payment and activate loan (Admin only)
    /// </summary>
    [HttpPost("{id}/verify-disbursement")]
    public async Task<IActionResult> VerifyDisbursement(Guid id, [FromQuery] string reference)
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

            // Verify payment with Paystack
            var paystackSecretKey = _configuration["Paystack:SecretKey"];
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {paystackSecretKey}");

            var response = await httpClient.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paystackResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseString);
                var status = paystackResponse.GetProperty("data").GetProperty("status").GetString();

                if (status == "success")
                {
                    // Mark loan as disbursed and active
                    loan.Status = LoanStatus.active;
                    loan.DisbursedAt = DateTime.UtcNow;
                    loan.ActivatedAt = DateTime.UtcNow;
                    
                    _context.Loans.Update(loan);
                    await _context.SaveChangesAsync();

                    var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);

                    return Ok(new ServiceResponse<AdminLoanResponse>
                    {
                        Success = true,
                        Message = "Loan disbursed successfully",
                        Code = "200",
                        Data = new AdminLoanResponse
                        {
                            Id = loan.Id,
                            BorrowerName = borrower?.UserName ?? "Unknown",
                            BorrowerEmail = borrower?.Email ?? "Unknown",
                            Principal = loan.Principal,
                            AmountDue = loan.AmountDue,
                            LoanTypeName = loan.LoanType?.Name ?? "",
                            Rate = loan.Rate,
                            Term = loan.Term,
                            Status = loan.Status,
                            CreatedAt = loan.CreatedAt,
                            ApprovedAt = loan.ApprovedAt,
                            DueAt = loan.DueAt,
                            DisbursedAt = loan.DisbursedAt,
                            ActivatedAt = loan.ActivatedAt
                        }
                    });
                }
                else
                {
                    return BadRequest(new ServiceResponse<AdminLoanResponse>
                    {
                        Success = false,
                        Message = "Payment not successful",
                        Code = "400",
                        Data = null,
                        Errors = new[] { $"Payment status: {status}" }
                    });
                }
            }

            return BadRequest(new ServiceResponse<AdminLoanResponse>
            {
                Success = false,
                Message = "Failed to verify payment",
                Code = "400",
                Data = null,
                Errors = new[] { responseString }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ServiceResponse<AdminLoanResponse>
            {
                Success = false,
                Message = "Error verifying disbursement",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// <summary>
    /// Initialize disbursement payment via Paystack (Admin only) - Step 1: Get payment URL
    /// </summary>
    [HttpPost("{id}/disburse/initialize")]
    public async Task<IActionResult> InitializeDisbursementPayment(Guid id)
    {
        try
        {
            var loan = await _context.Loans
                .Include(l => l.LoanType)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                return NotFound(new { success = false, message = "Loan not found" });
            }

            if (loan.Status != LoanStatus.approved)
            {
                return BadRequest(new { success = false, message = $"Only approved loans can be disbursed. Current status: {loan.Status}" });
            }

            var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);
            if (borrower == null)
            {
                return NotFound(new { success = false, message = "Borrower not found" });
            }

            // Initialize Paystack payment
            var paystackSecretKey = _configuration["Paystack:SecretKey"];
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {paystackSecretKey}");

            var callbackUrl = $"{_configuration["Frontend:BaseUrl"]}/admin/disbursement/callback?loanId={id}";
            
            var paystackRequest = new
            {
                email = borrower.Email,
                amount = (int)(loan.Principal * 100), // Convert to kobo
                reference = $"DISB-{loan.Id.ToString().Substring(0, 8).ToUpper()}-{DateTime.UtcNow.Ticks}",
                callback_url = callbackUrl,
                metadata = new
                {
                    loan_id = loan.Id.ToString(),
                    borrower_id = borrower.Id,
                    borrower_name = $"{borrower.FirstName} {borrower.LastName}",
                    loan_type = loan.LoanType?.Name ?? "",
                    disbursement = true
                }
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(paystackRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync("https://api.paystack.co/transaction/initialize", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paystackResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseString);
                var authUrl = paystackResponse.GetProperty("data").GetProperty("authorization_url").GetString();
                
                return Ok(new
                {
                    success = true,
                    message = "Disbursement payment initialized",
                    data = new
                    {
                        authorizationUrl = authUrl,
                        reference = paystackRequest.reference
                    }
                });
            }

            return BadRequest(new { success = false, message = "Failed to initialize payment", error = responseString });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error initializing disbursement", error = ex.Message });
        }
    }

    /// <summary>
    /// Verify disbursement payment and activate loan (Admin only) - Step 2: After Paystack callback
    /// </summary>
    [HttpGet("{id}/disburse/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyDisbursementPayment(Guid id, [FromQuery] string reference)
    {
        try
        {
            if (string.IsNullOrEmpty(reference))
            {
                return Redirect($"{_configuration["Frontend:BaseUrl"]}/admin/disbursement?error=No payment reference");
            }

            // Verify payment with Paystack
            var paystackSecretKey = _configuration["Paystack:SecretKey"];
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {paystackSecretKey}");

            var response = await httpClient.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Redirect($"{_configuration["Frontend:BaseUrl"]}/admin/disbursement?error=Payment verification failed");
            }

            var paystackResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseString);
            var status = paystackResponse.GetProperty("data").GetProperty("status").GetString();

            if (status == "success")
            {
                // Update loan status
                var loan = await _context.Loans.FindAsync(id);
                if (loan != null && loan.Status == LoanStatus.approved)
                {
                    loan.Status = LoanStatus.active;
                    loan.DisbursedAt = DateTime.UtcNow;
                    loan.ActivatedAt = DateTime.UtcNow;
                    
                    _context.Loans.Update(loan);
                    await _context.SaveChangesAsync();
                }

                return Redirect($"{_configuration["Frontend:BaseUrl"]}/admin/disbursement?success=Loan disbursed successfully&loanId={id}");
            }

            return Redirect($"{_configuration["Frontend:BaseUrl"]}/admin/disbursement?error=Payment failed");
        }
        catch (Exception ex)
        {
            return Redirect($"{_configuration["Frontend:BaseUrl"]}/admin/disbursement?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    /// <summary>
    /// Disburse an approved loan (Admin only) - Makes loan active
    /// </summary>
    [HttpPut("{id}/disburse")]
    public async Task<IActionResult> DisburseLoan(Guid id)
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

            if (loan.Status != LoanStatus.approved)
            {
                return BadRequest(new ServiceResponse<AdminLoanResponse>
                {
                    Success = false,
                    Message = "Cannot disburse loan",
                    Code = "400",
                    Data = null,
                    Errors = new[] { $"Only approved loans can be disbursed. Current status: {loan.Status}" }
                });
            }

            // Change status to active and set disbursement date
            loan.Status = LoanStatus.active;
            loan.DisbursedAt = DateTime.UtcNow;
            loan.ActivatedAt = DateTime.UtcNow;
            
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
                Message = "Loan disbursed successfully and activated",
                Code = "200",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<AdminLoanResponse>
            {
                Success = false,
                Message = "Error disbursing loan",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get disbursement statistics (Total Disbursed MTD, Pending Amount, Completed Today, Success Rate) - Admin only
    /// </summary>
    [HttpGet("disbursement/stats")]
    public async Task<IActionResult> GetDisbursementStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

            // Total Disbursed (MTD) - Sum of all active loans disbursed this month
            var totalDisbursedMTD = await _context.Loans
                .Where(l => l.Status == LoanStatus.active 
                    && l.DisbursedAt.HasValue 
                    && l.DisbursedAt.Value >= startOfMonth)
                .SumAsync(l => l.Principal);

            // Pending Amount - Sum of approved but not yet disbursed loans
            var pendingAmount = await _context.Loans
                .Where(l => l.Status == LoanStatus.approved)
                .SumAsync(l => l.Principal);

            // Completed Today - Count of loans disbursed today
            var completedToday = await _context.Loans
                .Where(l => l.Status == LoanStatus.active 
                    && l.DisbursedAt.HasValue 
                    && l.DisbursedAt.Value >= startOfToday)
                .CountAsync();

            // Success Rate - Percentage of successful disbursements (active) vs total (approved + active)
            var totalDisbursements = await _context.Loans
                .Where(l => l.Status == LoanStatus.approved || l.Status == LoanStatus.active)
                .CountAsync();

            var successfulDisbursements = await _context.Loans
                .Where(l => l.Status == LoanStatus.active && l.DisbursedAt.HasValue)
                .CountAsync();

            var successRate = totalDisbursements > 0 
                ? Math.Round((double)successfulDisbursements / totalDisbursements * 100, 1)
                : 0;

            return Ok(new ServiceResponse<object>
            {
                Success = true,
                Message = "Disbursement statistics retrieved successfully",
                Code = "200",
                Data = new
                {
                    TotalDisbursedMTD = totalDisbursedMTD,
                    PendingAmount = pendingAmount,
                    CompletedToday = completedToday,
                    SuccessRate = successRate
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<object>
            {
                Success = false,
                Message = "Error retrieving disbursement statistics",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all loans for disbursement management (approved and active loans) - Admin only
    /// </summary>
    [HttpGet("disbursement")]
    public async Task<IActionResult> GetLoansForDisbursement(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var query = _context.Loans
                .Include(l => l.LoanType)
                .Where(l => l.Status == LoanStatus.approved || l.Status == LoanStatus.active)
                .OrderByDescending(l => l.ApprovedAt);

            var totalCount = await query.CountAsync();
            var loans = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var loanResponses = new List<AdminLoanResponse>();
            foreach (var loan in loans)
            {
                var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);
                
                loanResponses.Add(new AdminLoanResponse
                {
                    Id = loan.Id,
                    BorrowerName = borrower != null ? $"{borrower.FirstName} {borrower.LastName}" : "Unknown",
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
                    DueAt = loan.DueAt,
                    DisbursedAt = loan.DisbursedAt,
                    ActivatedAt = loan.ActivatedAt
                });
            }

            return Ok(new ServiceResponse<List<AdminLoanResponse>>
            {
                Success = true,
                Message = "Loans for disbursement management retrieved successfully",
                Code = "200",
                Data = loanResponses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<List<AdminLoanResponse>>
            {
                Success = false,
                Message = "Error retrieving loans for disbursement",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }
}

