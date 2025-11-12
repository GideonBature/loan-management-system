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
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly FirstLendDbContext _context;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        FirstLendDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    /// <summary>
    /// Generate account number from user ID (for display purposes)
    /// Format: 309XXXXXXX where last 5 digits are masked
    /// </summary>
    private string GenerateAccountNumber(string userId)
    {
        // Get a numeric representation from the user ID
        var hash = userId.GetHashCode();
        var positiveHash = Math.Abs(hash);
        
        // Generate a 9-digit number starting with 309
        var accountNumber = $"309{positiveHash:D6}".Substring(0, 9);
        
        // Mask last 5 digits for display: 3090XXXXX
        return accountNumber.Substring(0, 4) + "XXXXX";
    }

    /// <summary>
    /// Get all users with pagination (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? userType = null)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            // Filter by user type if provided
            if (!string.IsNullOrEmpty(userType))
            {
                var role = await _roleManager.FindByNameAsync(userType);
                if (role != null)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(userType);
                    query = query.Where(u => usersInRole.Contains(u));
                }
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userResponses = new List<AdminUserResponse>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var totalLoans = await _context.Loans.CountAsync(l => l.BorrowerId == user.Id);
                var totalBorrowed = await _context.Loans
                    .Where(l => l.BorrowerId == user.Id)
                    .SumAsync(l => l.Principal);

            var activeLoan = await _context.Loans
                .Where(l => l.BorrowerId == user.Id && (l.Status == LoanStatus.active || l.Status == LoanStatus.pending || l.Status == LoanStatus.approved))
                .AnyAsync();

            var overdueLoan = await _context.Loans
                .Where(l => l.BorrowerId == user.Id && l.DueAt < DateTime.UtcNow && l.Status != LoanStatus.completed)
                .AnyAsync();

                var loanStatus = overdueLoan ? "Overdue" : (activeLoan ? "Active Loan" : "No Loan");

                userResponses.Add(new AdminUserResponse
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Address = user.Address ?? "",
                    AccountNumber = GenerateAccountNumber(user.Id),
                    UserType = user.UserType,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                    TotalLoans = totalLoans,
                    TotalBorrowed = totalBorrowed,
                    LoanStatus = loanStatus
                });
            }

            return Ok(new ServiceResponse<List<AdminUserResponse>>
            {
                Success = true,
                Message = "Users retrieved successfully",
                Code = "200",
                Data = userResponses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<List<AdminUserResponse>>
            {
                Success = false,
                Message = "Error retrieving users",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get user details by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ServiceResponse<AdminUserResponse>
                {
                    Success = false,
                    Message = "User not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "The requested user does not exist" }
                });
            }

            var totalLoans = await _context.Loans.CountAsync(l => l.BorrowerId == user.Id);
            var totalBorrowed = await _context.Loans
                .Where(l => l.BorrowerId == user.Id)
                .SumAsync(l => l.Principal);

            var activeLoan = await _context.Loans
                .Where(l => l.BorrowerId == user.Id && (l.Status == LoanStatus.active || l.Status == LoanStatus.pending || l.Status == LoanStatus.approved))
                .AnyAsync();

            var overdueLoan = await _context.Loans
                .Where(l => l.BorrowerId == user.Id && l.DueAt < DateTime.UtcNow && l.Status != LoanStatus.completed)
                .AnyAsync();

            var loanStatus = overdueLoan ? "Overdue" : (activeLoan ? "Active Loan" : "No Loan");

            var response = new AdminUserResponse
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address ?? "",
                AccountNumber = GenerateAccountNumber(user.Id),
                UserType = user.UserType,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                TotalLoans = totalLoans,
                TotalBorrowed = totalBorrowed,
                LoanStatus = loanStatus
            };

            return Ok(new ServiceResponse<AdminUserResponse>
            {
                Success = true,
                Message = "User retrieved successfully",
                Code = "200",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<AdminUserResponse>
            {
                Success = false,
                Message = "Error retrieving user",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create a new admin user (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAdminUser([FromBody] CreateAdminRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AdminUserResponse>
                {
                    Success = false,
                    Message = "Validation failed",
                    Code = "400",
                    Data = null,
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()
                });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new ServiceResponse<AdminUserResponse>
                {
                    Success = false,
                    Message = "User creation failed",
                    Code = "400",
                    Data = null,
                    Errors = new[] { "Email address is already in use" }
                });
            }

            var newUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FullName.Split(" ").First(),
                LastName = request.FullName.Contains(" ") ? string.Join(" ", request.FullName.Split(" ").Skip(1)) : "",
                PhoneNumber = request.PhoneNumber ?? "",
                UserType = UserType.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new ServiceResponse<AdminUserResponse>
                {
                    Success = false,
                    Message = "User creation failed",
                    Code = "400",
                    Data = null,
                    Errors = result.Errors.Select(e => e.Description).ToArray()
                });
            }

            // Assign Admin role by default for admin-created users
            await _userManager.AddToRoleAsync(newUser, "Admin");

            var totalLoans = 0;
            var totalBorrowed = 0m;

            var response = new AdminUserResponse
            {
                Id = newUser.Id,
                FullName = $"{newUser.FirstName} {newUser.LastName}",
                Email = newUser.Email ?? "",
                PhoneNumber = newUser.PhoneNumber ?? "",
                Address = newUser.Address ?? "",
                AccountNumber = GenerateAccountNumber(newUser.Id),
                UserType = newUser.UserType,
                Status = newUser.Status,
                CreatedAt = newUser.CreatedAt,
                TotalLoans = totalLoans,
                TotalBorrowed = totalBorrowed,
                LoanStatus = "No Loan"
            };

            return Ok(new ServiceResponse<AdminUserResponse>
            {
                Success = true,
                Message = "Admin user created successfully",
                Code = "201",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<AdminUserResponse>
            {
                Success = false,
                Message = "Error creating admin user",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update user status (Active/Inactive) (Admin only)
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] UpdateUserStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AdminUserResponse>
                {
                    Success = false,
                    Message = "Validation failed",
                    Code = "400",
                    Data = null,
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()
                });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ServiceResponse<AdminUserResponse>
                {
                    Success = false,
                    Message = "User not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "The requested user does not exist" }
                });
            }

            user.Status = Enum.Parse<UserStatus>(request.Status, ignoreCase: true);
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new ServiceResponse<AdminUserResponse>
                {
                    Success = false,
                    Message = "Status update failed",
                    Code = "400",
                    Data = null,
                    Errors = result.Errors.Select(e => e.Description).ToArray()
                });
            }

            var totalLoans = await _context.Loans.CountAsync(l => l.BorrowerId == user.Id);
            var totalBorrowed = await _context.Loans
                .Where(l => l.BorrowerId == user.Id)
                .SumAsync(l => l.Principal);

            var activeLoan = await _context.Loans
                .Where(l => l.BorrowerId == user.Id && (l.Status == LoanStatus.active || l.Status == LoanStatus.pending || l.Status == LoanStatus.approved))
                .AnyAsync();

            var overdueLoan = await _context.Loans
                .Where(l => l.BorrowerId == user.Id && l.DueAt < DateTime.UtcNow && l.Status != LoanStatus.completed)
                .AnyAsync();

            var loanStatus = overdueLoan ? "Overdue" : (activeLoan ? "Active Loan" : "No Loan");

            var response = new AdminUserResponse
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address ?? "",
                AccountNumber = GenerateAccountNumber(user.Id),
                UserType = user.UserType,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                TotalLoans = totalLoans,
                TotalBorrowed = totalBorrowed,
                LoanStatus = loanStatus
            };

            return Ok(new ServiceResponse<AdminUserResponse>
            {
                Success = true,
                Message = "User status updated successfully",
                Code = "200",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<AdminUserResponse>
            {
                Success = false,
                Message = "Error updating user status",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete a user (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ServiceResponse<object>
                {
                    Success = false,
                    Message = "User not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "The requested user does not exist" }
                });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new ServiceResponse<object>
                {
                    Success = false,
                    Message = "User deletion failed",
                    Code = "400",
                    Data = null,
                    Errors = result.Errors.Select(e => e.Description).ToArray()
                });
            }

            return Ok(new ServiceResponse<object>
            {
                Success = true,
                Message = "User deleted successfully",
                Code = "200",
                Data = null
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<object>
            {
                Success = false,
                Message = "Error deleting user",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get loan history for a specific user (Admin only)
    /// </summary>
    [HttpGet("{userId}/loans")]
    public async Task<IActionResult> GetUserLoanHistory(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ServiceResponse<List<AdminLoanResponse>>
                {
                    Success = false,
                    Message = "User not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { "The requested user does not exist" }
                });
            }

            var query = _context.Loans
                .Include(l => l.LoanType)
                .Where(l => l.BorrowerId == userId);

            var totalCount = await query.CountAsync();
            var loans = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var loanResponses = loans.Select(loan => new AdminLoanResponse
            {
                Id = loan.Id,
                BorrowerId = loan.BorrowerId,
                BorrowerName = $"{user.FirstName} {user.LastName}",
                BorrowerEmail = user.Email ?? "",
                LoanTypeName = loan.LoanType?.Name ?? "",
                Principal = loan.Principal,
                Rate = loan.Rate,
                Term = loan.Term,
                AmountDue = loan.AmountDue,
                Status = loan.Status,
                CreatedAt = loan.CreatedAt,
                DueAt = loan.DueAt,
                NextPaymentDate = loan.NextPaymentDate,
                EmploymentStatus = loan.EmploymentStatus,
                MonthlyIncome = loan.MonthlyIncome,
                Purpose = loan.Purpose
            }).ToList();

            return Ok(new ServiceResponse<List<AdminLoanResponse>>
            {
                Success = true,
                Message = "User loan history retrieved successfully",
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
                Message = "Error retrieving loan history",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get users by role (Admin only)
    /// </summary>
    [HttpGet("role/{roleName}")]
    public async Task<IActionResult> GetUsersByRole(
        string roleName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return NotFound(new ServiceResponse<List<AdminUserResponse>>
                {
                    Success = false,
                    Message = "Role not found",
                    Code = "404",
                    Data = null,
                    Errors = new[] { $"Role '{roleName}' does not exist" }
                });
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            var totalCount = usersInRole.Count;
            var paginatedUsers = usersInRole
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var userResponses = new List<AdminUserResponse>();
            foreach (var user in paginatedUsers)
            {
                var totalLoans = await _context.Loans.CountAsync(l => l.BorrowerId == user.Id);
                var totalBorrowed = await _context.Loans
                    .Where(l => l.BorrowerId == user.Id)
                    .SumAsync(l => l.Principal);

                var activeLoan = await _context.Loans
                    .Where(l => l.BorrowerId == user.Id && (l.Status == LoanStatus.active || l.Status == LoanStatus.pending || l.Status == LoanStatus.approved))
                    .AnyAsync();

                var overdueLoan = await _context.Loans
                    .Where(l => l.BorrowerId == user.Id && l.DueAt < DateTime.UtcNow && l.Status != LoanStatus.completed)
                    .AnyAsync();

                var loanStatus = overdueLoan ? "Overdue" : (activeLoan ? "Active Loan" : "No Loan");

                userResponses.Add(new AdminUserResponse
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Address = user.Address ?? "",
                    AccountNumber = GenerateAccountNumber(user.Id),
                    UserType = user.UserType,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                    TotalLoans = totalLoans,
                    TotalBorrowed = totalBorrowed,
                    LoanStatus = loanStatus
                });
            }

            return Ok(new ServiceResponse<List<AdminUserResponse>>
            {
                Success = true,
                Message = $"Users with role '{roleName}' retrieved successfully",
                Code = "200",
                Data = userResponses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ServiceResponse<List<AdminUserResponse>>
            {
                Success = false,
                Message = "Error retrieving users",
                Code = "500",
                Data = null,
                Errors = new[] { ex.Message }
            });
        }
    }
}
