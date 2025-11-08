using FirstLend.Application.Abstractions;
using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;
using FirstLend.Domain.Entities;
using FirstLend.Domain.Enums;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FirstLend.Infrastructure.Services
{
    public class LoanService : ILoanService
    {
        private readonly FirstLendDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoanService(FirstLendDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResponse<LoanResponse>> CreateAsync(string userId, CreateLoanRequest request)
        {
            try
            {
                // Verify user exists
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ServiceResponse<LoanResponse>
                    {
                        Success = false,
                        Message = "User not found",
                        Code = "USER_NOT_FOUND",
                        Data = null,
                        Errors = new[] { $"User with ID '{userId}' does not exist in the system" }
                    };
                }

                // Verify loan type exists by name
                var loanType = await _context.LoanTypes.FirstOrDefaultAsync(lt => lt.Name == request.LoanTypeName);
                if (loanType == null)
                {
                    return new ServiceResponse<LoanResponse>
                    {
                        Success = false,
                        Message = "Loan type not found",
                        Code = "LOAN_TYPE_NOT_FOUND",
                        Data = null,
                        Errors = new[] { $"Loan type '{request.LoanTypeName}' does not exist" }
                    };
                }

                // Use the provided rate or default to the loan type's interest rate
                var applicableRate = request.Rate.HasValue && request.Rate.Value > 0 ? request.Rate.Value : loanType.Interest;

                // Calculate monthly payment using amortization formula
                var monthlyRate = applicableRate / 100 / 12;
                var amountDue = request.Principal * (decimal)Math.Pow((double)(1 + monthlyRate), request.Term) * monthlyRate / 
                                ((decimal)Math.Pow((double)(1 + monthlyRate), request.Term) - 1);

                var loan = new Loan
                {
                    Id = Guid.NewGuid(),
                    BorrowerId = userId, // Use string directly
                    LoanTypeId = loanType.Id,
                    Status = LoanStatus.pending,
                    Principal = request.Principal,
                    Rate = applicableRate,
                    Term = request.Term,
                    OutstandingBalance = request.Principal,
                    AmountDue = Math.Round(amountDue, 2),
                    EmploymentStatus = request.EmploymentStatus,
                    MonthlyIncome = request.MonthlyIncome,
                    Purpose = request.Purpose,
                    CreatedAt = DateTime.UtcNow,
                    NextPaymentDate = DateTime.UtcNow.AddMonths(1),
                    DueAt = DateTime.UtcNow.AddMonths(request.Term)
                };

                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();

                return new ServiceResponse<LoanResponse>
                {
                    Success = true,
                    Message = "Loan application submitted successfully",
                    Code = "",
                    Data = new LoanResponse
                    {
                        Id = loan.Id,
                        BorrowerId = loan.BorrowerId,
                        LoanTypeId = loan.LoanTypeId,
                        Status = loan.Status,
                        Principal = loan.Principal,
                        Rate = loan.Rate,
                        Term = loan.Term,
                        OutstandingBalance = loan.OutstandingBalance,
                        AmountDue = loan.AmountDue,
                        EmploymentStatus = loan.EmploymentStatus,
                        MonthlyIncome = loan.MonthlyIncome,
                        NextPaymentDate = loan.NextPaymentDate,
                        Purpose = loan.Purpose,
                        CreatedAt = loan.CreatedAt,
                        DueAt = loan.DueAt,
                        BorrowerName = $"{user.FirstName} {user.LastName}".Trim(),
                        BorrowerEmail = user.Email!,
                        LoanTypeName = loanType.Name,
                        LoanTypeInterest = loanType.Interest
                    },
                    Errors = null
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<LoanResponse>
                {
                    Success = false,
                    Message = "Failed to create loan",
                    Code = "CREATE_FAILED",
                    Data = null,
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<ServiceResponse<LoanResponse>> GetByIdAsync(Guid id, string userId)
        {
            try
            {
                var loan = await _context.Loans
                    .Include(l => l.LoanType)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (loan == null)
                {
                    return new ServiceResponse<LoanResponse>
                    {
                        Success = false,
                        Message = "Loan not found",
                        Code = "NOT_FOUND",
                        Data = null,
                        Errors = new[] { $"Loan with ID {id} not found" }
                    };
                }

                // Check if user is the borrower or an admin
                var user = await _userManager.FindByIdAsync(userId);
                if (loan.BorrowerId != userId && user?.UserType != UserType.Admin)
                {
                    return new ServiceResponse<LoanResponse>
                    {
                        Success = false,
                        Message = "Unauthorized access",
                        Code = "UNAUTHORIZED",
                        Data = null,
                        Errors = new[] { "You don't have permission to view this loan" }
                    };
                }

                var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);

                return new ServiceResponse<LoanResponse>
                {
                    Success = true,
                    Message = "Loan retrieved successfully",
                    Code = "",
                    Data = new LoanResponse
                    {
                        Id = loan.Id,
                        BorrowerId = loan.BorrowerId,
                        LoanTypeId = loan.LoanTypeId,
                        Status = loan.Status,
                        Principal = loan.Principal,
                        Rate = loan.Rate,
                        Term = loan.Term,
                        OutstandingBalance = loan.OutstandingBalance,
                        AmountDue = loan.AmountDue,
                        EmploymentStatus = loan.EmploymentStatus,
                        MonthlyIncome = loan.MonthlyIncome,
                        NextPaymentDate = loan.NextPaymentDate,
                        Purpose = loan.Purpose,
                        CreatedAt = loan.CreatedAt,
                        DueAt = loan.DueAt,
                        BorrowerName = borrower != null ? $"{borrower.FirstName} {borrower.LastName}".Trim() : "",
                        BorrowerEmail = borrower?.Email ?? "",
                        LoanTypeName = loan.LoanType?.Name ?? "",
                        LoanTypeInterest = loan.LoanType?.Interest ?? 0
                    },
                    Errors = null
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<LoanResponse>
                {
                    Success = false,
                    Message = "Failed to retrieve loan",
                    Code = "RETRIEVAL_FAILED",
                    Data = null,
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<ServiceResponse<List<LoanResponse>>> GetUserLoansAsync(string userId, int page = 1, int pageSize = 10)
        {
            try
            {
                var totalCount = await _context.Loans.CountAsync(l => l.BorrowerId == userId);

                var loans = await _context.Loans
                    .Include(l => l.LoanType)
                    .Where(l => l.BorrowerId == userId)
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var user = await _userManager.FindByIdAsync(userId);
                var borrowerName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "";
                var borrowerEmail = user?.Email ?? "";

                var response = loans.Select(loan => new LoanResponse
                {
                    Id = loan.Id,
                    BorrowerId = loan.BorrowerId,
                    LoanTypeId = loan.LoanTypeId,
                    Status = loan.Status,
                    Principal = loan.Principal,
                    Rate = loan.Rate,
                    Term = loan.Term,
                    OutstandingBalance = loan.OutstandingBalance,
                    AmountDue = loan.AmountDue,
                    EmploymentStatus = loan.EmploymentStatus,
                    MonthlyIncome = loan.MonthlyIncome,
                    NextPaymentDate = loan.NextPaymentDate,
                    Purpose = loan.Purpose,
                    CreatedAt = loan.CreatedAt,
                    DueAt = loan.DueAt,
                    BorrowerName = borrowerName,
                    BorrowerEmail = borrowerEmail,
                    LoanTypeName = loan.LoanType?.Name ?? "",
                    LoanTypeInterest = loan.LoanType?.Interest ?? 0
                }).ToList();

                return new ServiceResponse<List<LoanResponse>>
                {
                    Success = true,
                    Message = "Loans retrieved successfully",
                    Code = "",
                    Data = response,
                    Errors = null,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<LoanResponse>>
                {
                    Success = false,
                    Message = "Failed to retrieve loans",
                    Code = "RETRIEVAL_FAILED",
                    Data = null,
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<ServiceResponse<List<LoanResponse>>> GetAllLoansAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var totalCount = await _context.Loans.CountAsync();

                var loans = await _context.Loans
                    .Include(l => l.LoanType)
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new List<LoanResponse>();

                foreach (var loan in loans)
                {
                    var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);
                    response.Add(new LoanResponse
                    {
                        Id = loan.Id,
                        BorrowerId = loan.BorrowerId,
                        LoanTypeId = loan.LoanTypeId,
                        Status = loan.Status,
                        Principal = loan.Principal,
                        Rate = loan.Rate,
                        Term = loan.Term,
                        OutstandingBalance = loan.OutstandingBalance,
                        AmountDue = loan.AmountDue,
                        EmploymentStatus = loan.EmploymentStatus,
                        MonthlyIncome = loan.MonthlyIncome,
                        NextPaymentDate = loan.NextPaymentDate,
                        Purpose = loan.Purpose,
                        CreatedAt = loan.CreatedAt,
                        DueAt = loan.DueAt,
                        BorrowerName = borrower != null ? $"{borrower.FirstName} {borrower.LastName}".Trim() : "",
                        BorrowerEmail = borrower?.Email ?? "",
                        LoanTypeName = loan.LoanType?.Name ?? "",
                        LoanTypeInterest = loan.LoanType?.Interest ?? 0
                    });
                }

                return new ServiceResponse<List<LoanResponse>>
                {
                    Success = true,
                    Message = "All loans retrieved successfully",
                    Code = "",
                    Data = response,
                    Errors = null,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<LoanResponse>>
                {
                    Success = false,
                    Message = "Failed to retrieve loans",
                    Code = "RETRIEVAL_FAILED",
                    Data = null,
                    Errors = new[] { ex.Message }
                };
            }
        }
    }
}
